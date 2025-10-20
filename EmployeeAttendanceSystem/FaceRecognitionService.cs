using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System.Diagnostics;



namespace EmployeeAttendanceSystem
{
    internal class FaceRecognitionService : IDisposable
    {
        //class variables
        
        private readonly InferenceSession _session;
        private readonly Dictionary<int, string> _labelMap;//maps numeric labels to person names
        private readonly Dictionary<int, List<float[]>> _faceEmbeddings; //Store embeddings per person
        
        private bool _isDisposed;


        // 2. Constructor
        public FaceRecognitionService(string modelPath)
        {
            
            //load the ONNX model from right path:
            _session = new InferenceSession(modelPath);

            //lets see the input and output names:
            Debug.WriteLine("Input Names:");
            foreach(var input in _session.InputMetadata)
            {
                Debug.WriteLine($"  - {input.Key} (Shape: {string.Join(",", input.Value.Dimensions)})");
            }
            Debug.WriteLine("Output names:");
            foreach (var output in _session.OutputMetadata)
            {
                Debug.WriteLine($"  - {output.Key} (Shape: {string.Join(",", output.Value.Dimensions)})");
            }
            Debug.WriteLine("==============================");

            // Initialize data structures
            _labelMap = new Dictionary<int, string>();
            _faceEmbeddings = new Dictionary<int, List<float[]>>();

        }

        public void RegisterFacesFromDataset(string datasetPath)
        {
            Debug.WriteLine("==============================");
            Debug.WriteLine("=Initializing the embeeding fase==");

            _labelMap.Clear();
            _faceEmbeddings.Clear();

            if (!Directory.Exists(datasetPath))
            {
                MessageBox.Show($"Dataset directory not found:{datasetPath}");
                Debug.WriteLine("==============================");
                Debug.WriteLine("=Dataset directory not found==");
                return;
            }
            
            var personFolders = Directory.GetDirectories(datasetPath).OrderBy(f => f);

            foreach (var folder in personFolders)
            {
                string folderName = Path.GetFileName(folder);
                // Parse the person's ID from the folder name (e.g., "Person1")
                if (!int.TryParse(folderName.Replace("Person", ""), out int personId))
                {
                    Debug.WriteLine($"Skipping invalid folder: {folder}");
                    continue;
                }

                Debug.WriteLine($"Valid Folder: {folder} with folderName: {folderName} with ID: {personId}");

                _labelMap[personId] = folderName; // e.g., 1 -> "Person1"
                var embeddingsForPerson = new List<float[]>();
                
                // int count = 0;
                foreach (var file in Directory.GetFiles(folder))
                {
                    //Debug.WriteLine($"{count} - Got the file: {file} from folder: {folder}...");
                    //count++;
                    
                    //if (!IsImageFile(file)) continue;

                    try
                    {
                        using (var image = new Mat(file))
                        {
                            if (image.Empty()) continue;
                            Debug.WriteLine("==============================");
                            Debug.WriteLine("=...PreprocessForFaceNet!==");
                            // need process the image to the input that onnx expects - RGB, size(160,160),and normalidez pixels
                            using (var processedFace = PreprocessForFaceNet(image))//preprocess takes the image input - outputs 
                            {
                                if (processedFace != null)
                                {
                                    Debug.WriteLine($"processedFace is not null in file: {file}");
                                    float[] embedding = GetFaceEmbedding(processedFace);
                                    embeddingsForPerson.Add(embedding);
                                }
                                else
                                {
                                    Debug.WriteLine($"Error in adding the preprocessedFace in {file}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error processing {file}: {ex.Message}");
                    }
                }

                if (embeddingsForPerson.Count > 0)
                {
                    _faceEmbeddings[personId] = embeddingsForPerson;
                }
            }
        }
        
        private Mat PreprocessForFaceNet(Mat image)
        {
            Mat rgb = new Mat();
            Cv2.CvtColor(image, rgb, ColorConversionCodes.BGR2RGB);

            //FaceNet expects (e.g., 160x160)
            Mat resized = new Mat();
            Cv2.Resize(rgb, resized, new OpenCvSharp.Size(160, 160));

            // 3. Convert pixel values [0, 255]- ([-1, 1] or [0, 1])
            Mat normalized = new Mat();
            // Example: Normalize to [-1, 1]. Check your specific model's requirements.
            //how do I know that I converted to the right format?
            resized.ConvertTo(normalized, MatType.CV_32FC3, 2.0 / 255.0, -1.0);

            return normalized.Clone(); // Return a clone to avoid disposal issues
        }

        //Get the face embedding from the ONNX model
        private float[] GetFaceEmbedding(Mat processedFace)
        {
            Debug.WriteLine($"=== GetFaceEmbedding Called ===");
            Debug.WriteLine($"Session is null: {_session == null}");
            Debug.WriteLine($"Session is disposed: {_isDisposed}");

            if (_session == null || _isDisposed)
            {
                throw new InvalidOperationException("ONNX session is not available");
            }

            // Print available input names
            Debug.WriteLine("Available input names:");
            foreach (var name in _session.InputMetadata.Keys)
            {
                Debug.WriteLine($"  - {name}");
            }

            // Convert the OpenCV Mat to a tensor for ONNX Runtime
            var inputTensor = ConvertMatToTensor(processedFace);
            string inputName = _session.InputMetadata.Keys.First();

            // Create the model input. The input name must match your model.
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(inputName, inputTensor)
            };

            // Run inference
            using (var results = _session.Run(inputs))
            {
                // Get the first output
                string outputName = _session.OutputMetadata.Keys.First();
                var embeddingTensor = results.FirstOrDefault(r => r.Name == outputName)?.AsTensor<float>();

                if (embeddingTensor == null)
                {
                    throw new InvalidOperationException($"Could not find output '{outputName}' in model results");
                }

                return embeddingTensor.ToArray();
            }
        }

        // Converts OpenCV Mat -> ONNX Runtime Tensor - CORRECTED VERSION
        private DenseTensor<float> ConvertMatToTensor(Mat image)
        {

            Debug.WriteLine($"Image dimensions: {image.Rows}x{image.Cols}");
            Debug.WriteLine($"Image channels: {image.Channels()}");
            Debug.WriteLine($"Image type: {image.Type()}");
            var tensor = new DenseTensor<float>(new[] { 1, 160, 160, 3 });

            if (image.Rows != 160 || image.Cols != 160)
            {
                throw new ArgumentException("Input image must be 160x160 pixels.");
            }

            if (image.Channels() != 3)
            {
                throw new ArgumentException("Input image must have 3 channels (RGB).");
            }

            // Get the indexer for Vec3f
            var indexer = image.GetGenericIndexer<Vec3f>();

            // Copy data to tensor with correct channel ordering
            for (int y = 0; y < 160; y++)
            {
                for (int x = 0; x < 160; x++)
                {
                    var pixel = indexer[y, x];
                    tensor[0, y, x, 0] = pixel.Item0; // R
                    tensor[0, y, x, 1] = pixel.Item1; // G  
                    tensor[0, y, x, 2] = pixel.Item2; // B
                }
            }
            return tensor;
        }

        //MAKE PREDICTIONS:
        public (string Name, double Confidence) Recognize(Mat inputImage)
        {
            
            using (var processedFace = PreprocessForFaceNet(inputImage))
            {
                if (processedFace == null)
                {
                    return ("Invalid or no face found", 0.0);
                }

                
                float[] queryEmbedding = GetFaceEmbedding(processedFace);

                // 3. Find the best match in the database
                return FindBestMatch(queryEmbedding);
            }
        }
        // **Finds the closest matching embedding in the database**
        private (string Name, double Confidence) FindBestMatch(float[] queryEmbedding)
        {
            double bestDistance = double.MaxValue;
            int bestMatchId = -1;

            foreach (var personData in _faceEmbeddings)
            {
                foreach (var storedEmbedding in personData.Value)
                {
                    // Calculate the distance (dissimilarity) between embeddings
                    double distance = CosineDistance(queryEmbedding, storedEmbedding);

                    // A smaller distance means a better match
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestMatchId = personData.Key;
                    }
                }
            }

            // 4. Convert distance to a confidence percentage and apply a threshold
            double confidence = Math.Max(0, 100 - bestDistance * 100);

            // You need to define a threshold. If the best match isn't good enough, return "Unknown".
            if (confidence < 60) // Adjust this threshold based on testing
            {
                return ("Unknown", confidence);
            }

            return (_labelMap[bestMatchId], confidence);
        }

        private double CosineDistance(float[] v1, float[] v2)
        {
            double dot = 0.0;
            double mag1 = 0.0;
            double mag2 = 0.0;

            for (int i = 0; i < v1.Length; i++)
            {
                dot += v1[i] * v2[i];
                mag1 += v1[i] * v1[i];
                mag2 += v2[i] * v2[i];
            }

            // Cosine Similarity = dot / (sqrt(mag1) * sqrt(mag2))
            // Cosine Distance = 1 - Cosine Similarity
            return 1.0 - (dot / (Math.Sqrt(mag1) * Math.Sqrt(mag2)));
        }

        public bool HasEmbeddings()
        {
            return _faceEmbeddings != null && _faceEmbeddings.Count > 0;
        }

        private bool IsImageFile(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            return ext == ".jpg" || ext == ".jpeg";
        }

        // 5. Cleanup
        public void Dispose()
        {
            if (_isDisposed) return;
            _session?.Dispose();
            _isDisposed = true;
        }
    }
}