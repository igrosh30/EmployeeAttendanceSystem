using OpenCvSharp;
using OpenCvSharp.Extensions; // For BitmapConverter
using OpenCvSharp.Face;//contains the face recognition algorithms
using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;         // For System.Drawing.Size if needed
using System.IO;//for file folders operations
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Size = OpenCvSharp.Size; // This tells C# to use OpenCV's Size by default

//We will use OpenCV's CascadeClassifier

namespace EmployeeAttendanceSystem
{
    internal class FaceRecognitionService : IDisposable
    {
        //class variables
        private readonly LBPHFaceRecognizer _recognizer;
        private readonly Dictionary<int, string> _labelMap;//maps numeric labels to person names

        private bool _isDisposed;


        private List<Mat> _trainingImages = new List<Mat>();
        private List<int> _trainingLabels = new List<int>();

        public bool NeedsTraining => _trainingImages.Count == 0 || _recognizer == null;

        //add thecasCade classifier
        private CascadeClassifier _faceCascade;

        // 2. Constructor
        public FaceRecognitionService()
        {
            try
            {
                _recognizer = LBPHFaceRecognizer.Create(
                radius: 1,
                neighbors: 8,
                gridX: 8,
                gridY: 8,
                threshold: double.PositiveInfinity);

                //lets inicialize face detector
                string cascadePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                                    "haarcascade_frontalface_default.xml");
                if (!File.Exists(cascadePath))
                    throw new FileNotFoundException("Haar cascade file missing");
                _faceCascade = new CascadeClassifier(cascadePath);
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Initialization failed: {ex.Message}");
                throw; // Important for DI containers
            }
            
            _labelMap = new Dictionary<int, string>();

            //shoud I inicialize here the _trainingImages and _trainingLabels?

        }



        public void SaveModel(string modelPath)
        {
            _recognizer.Write(modelPath);
            File.WriteAllText(modelPath + ".labels",
                string.Join(",", _labelMap.Select(x => $"{x.Key}:{x.Value}")));
        }

        public bool LoadModel(string modelPath)
        {
            if (!File.Exists(modelPath)) return false;//model does not exist

            _recognizer.Read(modelPath);
            var labelData = File.ReadAllText(modelPath + ".labels");

            _labelMap.Clear();
            foreach (var item in labelData.Split(','))
            {
                var parts = item.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out int key))
                {
                    _labelMap[key] = parts[1];
                }
            }

            return true;
        }


        //add a method to Preprocess Images:

        private Mat PreprocessImage(Mat input)
        {
            // 1. Convert to grayscale
            Mat gray = new Mat();
            Cv2.CvtColor(input, gray, ColorConversionCodes.BGR2GRAY);

            // 2. Face detection (optional but recommended)
            try
            {
                using (var faceCascade = new CascadeClassifier("haarcascade_frontalface_default.xml"))
                {
                    Rect[] faces = faceCascade.DetectMultiScale(gray);
                    if (faces.Length > 0)
                    {
                        // Crop to the largest face
                        Rect faceRect = faces.OrderByDescending(f => f.Width * f.Height).First();
                        gray = new Mat(gray, faceRect);
                    }
                }
            }
            catch
            {
                // Continue without face detection if cascade fails
            }

            // 3. Resize to standard dimensions
            Mat resized = new Mat();
            Cv2.Resize(gray, resized, new OpenCvSharp.Size(100, 100));

            // 4. Histogram equalization
            Mat equalized = new Mat();
            Cv2.EqualizeHist(resized, equalized);

            // 5. Noise reduction
            Mat denoised = new Mat();
            Cv2.GaussianBlur(equalized, denoised, new OpenCvSharp.Size(3, 3), 0);

            // 6. Contrast enhancement (optional)
            using (var clahe = Cv2.CreateCLAHE(2.0, new OpenCvSharp.Size(8, 8)))
            {
                Mat enhanced = new Mat();
                clahe.Apply(denoised, enhanced);
                return enhanced.Clone();
            }

            // If not using CLAHE, return denoised
            return denoised.Clone();
        }

        //First we will check if the dataSet is valid:
        
        //1 - Method that finds faces on image
        public Rect? DetectFace(Mat image)
        {
            //use Cascade Classifier, converts to grayscale the images
            using (var gray = image.CvtColor(ColorConversionCodes.BGR2GRAY))
            {
                // Detect faces
                var faces = _faceCascade.DetectMultiScale(
                    gray,
                    scaleFactor: 1.1,
                    minNeighbors: 5,
                    minSize: new Size(60, 60));

                //returns rectangle around largest face or null(if not found)
                return faces.Length > 0 ? (Rect?)faces[0] : null;
            }
        }

        //2 - returns the process face, without noise and resized
        public Mat PreprocessFace(Mat image, Rect face)
        {
            // 1. Crop to face region
            using (var faceImg = new Mat(image, face))
            // 2. Convert to grayscale
            using (var gray = faceImg.CvtColor(ColorConversionCodes.BGR2GRAY))
            {
                // 3. Resize to standard size
                var resized = new Mat();
                Cv2.Resize(gray, resized, new Size(100, 100));

                // 4. Histogram equalization
                Cv2.EqualizeHist(resized, resized);

                // 5. Noise reduction
                Cv2.GaussianBlur(resized, resized, new Size(3, 3), 0);

                // 6. Return processed face
                return resized.Clone();
            }
        }

        //3- DataSetValidation
        public void TestDetectionOnDataset(string datasetPath)
        {
            // Add this at the start of the method
            string logPath = Path.Combine(Application.StartupPath, "face_detection_log.txt");
            File.WriteAllText(logPath, $"Face Detection Log - {DateTime.Now}\n\n");
            foreach (var folder in Directory.GetDirectories(datasetPath))
            {
                foreach (var file in Directory.GetFiles(folder)) // Test first 3 images
                {
                    using (var image = new Mat(file))
                    {
                        var faceRect = DetectFace(image);
                        if (faceRect.HasValue)
                        {
                            //File.AppendAllText(logPath, $"Detected face in: {file}\n");
                            
                              using (var face = new Mat(image, faceRect.Value))
                            {
                                Cv2.ImShow("Detected Face", face);
                                Cv2.WaitKey(2000); // show the image for 2 seconds
                            }
                              
                             

                        }
                        else
                        {
                            //string message = $"No face in: {file}";
                            //Debug.WriteLine(message);
                            //File.AppendAllText(logPath, message + "\n");
                            //Debug.WriteLine($"No face in: {file}");
                        }
                    }
                }
            }
            Cv2.DestroyAllWindows();

            //File.AppendAllText(logPath, "\nDetection completed.");
           // MessageBox.Show($"Detection log saved to:\n{logPath}");
        }

        //4 - Preprocessing Verification: shows original vs preprocessed face side-by-side
        public void CheckPreprocessing(string imagePath)
        {
            using (var image = new Mat(imagePath))
            {
                var faceRect = DetectFace(image);
                if (faceRect.HasValue)
                {
                    using (var processed = PreprocessFace(image, faceRect.Value))
                    {
                        // Original face
                        using (var face = new Mat(image, faceRect.Value))
                        {
                            Cv2.ImShow("Original Face", face);
                        }

                        // Processed face
                        Cv2.ImShow("Processed Face", processed);
                        Cv2.WaitKey(0);
                    }
                }
            }
            Cv2.DestroyAllWindows();
        }

        //3- Load the good dataSet:
        public void LoadDataset(string datasetPath)
        {
            _labelMap.Clear();
            _trainingImages.Clear();
            _trainingLabels.Clear();

            if (!Directory.Exists(datasetPath))
            {
                MessageBox.Show($"Dataset directory not found: {datasetPath}");
                return;
            }

            var personFolders = Directory.GetDirectories(datasetPath)
                                       .OrderBy(f => f);

            foreach (var folder in personFolders)
            {
                string folderName = Path.GetFileName(folder);
                if (!int.TryParse(folderName.Replace("Person", ""), out int personIndex))
                {
                    Debug.WriteLine($"Skipping invalid folder: {folder}");
                    continue;
                }

                _labelMap[personIndex] = $"Person{personIndex}";
                int addedSamples = 0;

                foreach (var file in Directory.GetFiles(folder))
                {
                    if (!IsImageFile(file)) continue;

                    try
                    {
                        using (var image = new Mat(file))
                        {
                            if (image.Empty())
                            {
                                Debug.WriteLine($"Invalid/corrupt image: {file}");
                                continue;//skip to next file
                            }
                            // 1. Detect face
                            var faceRect = DetectFace(image);//null or cropped images with a face in it
                            if (!faceRect.HasValue)
                            {
                                Debug.WriteLine($"No face detected in: {file}");
                                continue;
                            }

                            // 2. Preprocess face
                            Mat processedFace = PreprocessFace(image, faceRect.Value);
                            _trainingImages.Add(processedFace);
                            _trainingLabels.Add(personIndex);
                            addedSamples++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error processing {file}: {ex.Message}");
                    }
                }
                if(addedSamples < 5)
                {
                    Debug.WriteLine($"Warning: Person {personIndex} has only {addedSamples} valid images");
                }
            }
        }


        //Train the Model
        public void TrainRecognizer()
        {
            if (_trainingImages.Count == 0)
                throw new InvalidOperationException("No training data loaded");

            _recognizer.Update(_trainingImages.ToArray(), _trainingLabels.ToArray());
        }

        public (string Name, double Confidence) Recognize(Mat inputImage)
        {
            // 1. Detect face
            var faceRect = DetectFace(inputImage);
            if (faceRect == null)
            {
                return ("No Face Detected", 0);
            }

            // 2. Preprocess the face
            using (var processedFace = PreprocessFace(inputImage, faceRect.Value))
            {
                // 3. Recognize the face
                //predict method returns
                //1 - label: predicted persons ID
                //2- Confidence: distance metric (lower + better match)
                _recognizer.Predict(processedFace, out int label, out double confidence);
                

                // 4. Return results (lower confidence = better match)
                if (confidence > 70) // Threshold can be adjusted
                    return ("Unknown", confidence);

                return _labelMap.TryGetValue(label, out string name)
                    ? (name, confidence)
                    : ("Unknown", confidence);
            }
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
            _recognizer?.Dispose();
            _faceCascade?.Dispose();
            _trainingImages.ForEach(m => m?.Dispose());
            _isDisposed = true;
        }
    }
}