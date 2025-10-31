// Services/FaceRecognitionService.cs
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System.Diagnostics;

namespace EmployeeAttendanceApi.Services
{
    public class FaceRecognitionService : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly Dictionary<int, string> _labelMap = new();
        private readonly Dictionary<int, List<float[]>> _faceEmbeddings = new();
        private readonly ILogger<FaceRecognitionService> _log;
        private bool _disposed;

        public FaceRecognitionService(string modelPath, ILogger<FaceRecognitionService> log)
        {
            _log = log;
            if (!File.Exists(modelPath))
                throw new FileNotFoundException($"ONNX model not found: {modelPath}");

            _session = new InferenceSession(modelPath);

            /* Log model metadata (helps debugging)
            _log.LogInformation("=== ONNX Model Metadata ===");
            foreach (var i in _session.InputMetadata)
                _log.LogInformation("Input: {Name}  Shape: [{Shape}]", i.Key, string.Join(",", i.Value.Dimensions));
            foreach (var o in _session.OutputMetadata)
                _log.LogInformation("Output: {Name}  Shape: [{Shape}]", o.Key, string.Join(",", o.Value.Dimensions));
            _log.LogInformation("==============================");
            */
        }

        // -------------------------------------------------------------
        // 1. TRAIN – register all faces from a folder 
        // -------------------------------------------------------------
        public async Task RegisterAsync(string datasetPath, CancellationToken ct = default)
        {
            _log.LogInformation("=== Starting registration ===");
            _labelMap.Clear();
            _faceEmbeddings.Clear();

            if (!Directory.Exists(datasetPath))
                throw new DirectoryNotFoundException($"Dataset not found: {datasetPath}");

            var folders = Directory.GetDirectories(datasetPath).OrderBy(f => f);
            //Parses folder name (Person5 → id = 5)
            foreach (var folder in folders)
            {
                var name = Path.GetFileName(folder);
                if (!int.TryParse(name.Replace("Person", ""), out int id))
                {
                    _log.LogWarning("Skipping invalid folder: {Folder}", folder);
                    continue;
                }

                _labelMap[id] = name;
                var embeddings = new List<float[]>();

                foreach (var file in Directory.GetFiles(folder).Where(IsImageFile))
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.Yield(); // tiny async yield

                    using var img = Cv2.ImRead(file);
                    if (img.Empty()) continue;

                    using var face = Preprocess(img);
                    if (face == null) continue;

                    var emb = GetEmbedding(face);
                    embeddings.Add(emb);
                }

                if (embeddings.Count > 0)
                    _faceEmbeddings[id] = embeddings;
            }

            _log.LogInformation("Registration complete. {Count} persons.", _labelMap.Count);
        }

        // -------------------------------------------------------------
        // 2. RECOGNIZE – single image
        // -------------------------------------------------------------
        public async Task<(string Name, double Confidence)> RecognizeAsync(Mat image, CancellationToken ct = default)
        {
            await Task.Yield();
            using var face = Preprocess(image);
            if (face == null)
                return ("Invalid face", 0.0);

            var query = GetEmbedding(face);
            return FindBestMatch(query);
        }

        // -------------------------------------------------------------
        // Helper methods (unchanged, only minor logging)
        // -------------------------------------------------------------
        private Mat Preprocess(Mat src)
        {
            if (src.Empty() || src.Channels() != 3) return null;

            using Mat rgb = new Mat();
            using Mat resized = new Mat();
            using Mat norm = new Mat();

            Cv2.CvtColor(src, rgb, ColorConversionCodes.BGR2RGB);
            Cv2.Resize(rgb, resized, new Size(160, 160));
            Cv2.ConvertScaleAbs(resized, norm, 2.0 / 255.0, -1.0);

            return norm.Clone();
        }

        private float[] GetEmbedding(Mat face)
        {
            var tensor = MatToTensor(face);
            string inputName = _session.InputMetadata.Keys.First();

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(inputName, tensor)
            };

            using var results = _session.Run(inputs);
            string outputName = _session.OutputMetadata.Keys.First();
            var outTensor = results.FirstOrDefault(r => r.Name == outputName)
                           ?.AsTensor<float>()
                           ?? throw new InvalidOperationException($"Output '{outputName}' not found");

            return outTensor.ToArray();
        }

        private DenseTensor<float> MatToTensor(Mat img)
        {
            if (img.Rows != 160 || img.Cols != 160 || img.Channels() != 3)
                throw new ArgumentException("Image must be 160×160×3 (RGB)");

            var tensor = new DenseTensor<float>(new[] { 1, 160, 160, 3 });
            var indexer = img.GetGenericIndexer<Vec3f>();

            for (int y = 0; y < 160; y++)
                for (int x = 0; x < 160; x++)
                {
                    var p = indexer[y, x];
                    tensor[0, y, x, 0] = p.Item0; // R
                    tensor[0, y, x, 1] = p.Item1; // G
                    tensor[0, y, x, 2] = p.Item2; // B
                }
            return tensor;
        }

        private (string Name, double Confidence) FindBestMatch(float[] query)
        {
            if (!_faceEmbeddings.Any())
                return ("No embeddings registered", 0.0);

            double bestDist = double.MaxValue;
            int bestId = -1;

            foreach (var kv in _faceEmbeddings)
                foreach (var stored in kv.Value)
                {
                    double d = CosineDistance(query, stored);
                    if (d < bestDist) { bestDist = d; bestId = kv.Key; }
                }

            double confidence = Math.Max(0, 100 - bestDist * 100);
            return confidence < 60 ? ("Unknown", confidence) : (_labelMap[bestId], confidence);
        }

        private double CosineDistance(float[] a, float[] b)
        {
            double dot = 0, ma = 0, mb = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                ma += a[i] * a[i];
                mb += b[i] * b[i];
            }
            return 1.0 - dot / (Math.Sqrt(ma) * Math.Sqrt(mb));
        }

        private bool IsImageFile(string p) =>
            Path.GetExtension(p).ToLower() is ".jpg" or ".jpeg";

        public void Dispose()
        {
            if (_disposed) return;
            _session?.Dispose();
            _disposed = true;
        }
    }
}