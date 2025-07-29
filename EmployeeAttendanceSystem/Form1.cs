using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace EmployeeAttendanceSystem
{
    public partial class Form1 : Form
    {
        private Mat latestFrame;
        private FaceRecognitionService _faceService;
        private const string ModelPath = "face_model.yml";
        private string selectedImagePath;

        public Form1()
        {
            InitializeComponent();
            InitializeFaceRecognitionService();
        }

        private void InitializeFaceRecognitionService()
        {
            _faceService = new FaceRecognitionService();

            string modelPath = Path.Combine(Application.StartupPath, ModelPath);
            MessageBox.Show($"Model will be saved to:\n{modelPath}\n\n" +
                         $"Current directory:\n{Environment.CurrentDirectory}");

            if (!_faceService.LoadModel(ModelPath))
            {
                buttonTrain.Enabled = true;
                labelUpload.Text = "No trained model found. Please train first.";
            }
            else
            {
                labelUpload.Text = "Pretrained model loaded successfully.";
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            latestFrame?.Dispose();
            _faceService?.Dispose();
        }

        private void buttonUpload_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    selectedImagePath = openFileDialog1.FileName;
                    pictureBox1.Image?.Dispose();
                    latestFrame?.Dispose();

                    pictureBox1.Image = new Bitmap(selectedImagePath);
                    latestFrame = BitmapConverter.ToMat((Bitmap)pictureBox1.Image);
                    buttonRecognizer.Enabled = true;

                    labelUpload.Text = "Image loaded successfully. Click Recognize.";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}");
                    buttonRecognizer.Enabled = false;
                    labelUpload.Text = "Error loading image.";
                }
            }
        }

        private void buttonRecognizer_Click(object sender, EventArgs e)
        {
            if (latestFrame == null || latestFrame.Empty() )
            {
                MessageBox.Show("Please upload an image first!");
                return;
            }

            try
            {
                var (name, confidence) = _faceService.Recognize(latestFrame);
                double displayConfidence = Math.Max(0, 100 - confidence);

                MessageBox.Show($"Recognized: {name}\nConfidence: {displayConfidence:F1}%");
                labelUpload.Text = $"Recognized: {name} (Confidence: {displayConfidence:F1}%)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Recognition failed: {ex.Message}");
                labelUpload.Text = "Recognition error occurred.";
            }
        }

        private void buttonTrain_Click(object sender, EventArgs e)
        {
            buttonTrain.Enabled = false;//once you trainned you cannot train again
            labelUpload.Text = "Testing dataset...";
            Application.DoEvents(); // Force UI update

            try
            {
                string datasetPath = Path.Combine(Application.StartupPath, "FacesDataset");
                //when I call this method it doesnt do much right?! I can erase this...
                _faceService.TestDetectionOnDataset(datasetPath);

                //here I load my dataset
                labelUpload.Text = "Loading dataset...";
                Application.DoEvents();
                _faceService.LoadDataset(datasetPath);

                labelUpload.Text = "Training model...";
                Application.DoEvents();
                _faceService.TrainRecognizer();

                labelUpload.Text = "Saving model...";
                Application.DoEvents();
                _faceService.SaveModel(ModelPath);

                labelUpload.Text = "Training complete!";
            }
            finally
            {
                buttonTrain.Enabled = true;
            }
        }

        // Add this method to test preprocessing on demand
        private void TestPreprocessing()
        {
            if (latestFrame != null)
            {
                string tempPath = Path.Combine(Path.GetTempPath(), "test_image.jpg");
                latestFrame.ImWrite(tempPath);
                _faceService.CheckPreprocessing(tempPath);
                File.Delete(tempPath);
            }
            else
            {
                MessageBox.Show("Please upload an image first!");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void buttonTestDataset_Click(object sender, EventArgs e)
        {
            string datasetPath = Path.Combine(Application.StartupPath, "FacesDataset");
            if (!Directory.Exists(datasetPath))
            {
                MessageBox.Show($"Dataset folder not found at:\n{datasetPath}");
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                _faceService.TestDetectionOnDataset(datasetPath);
                MessageBox.Show($"Dataset test completed.\nCheck debug output for details.");
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void buttonTestPreprocess_Click(object sender, EventArgs e)
        {
            if (latestFrame == null)
            {
                MessageBox.Show("Please upload an image first!");
                return;
            }

            try
            {
                string tempPath = Path.GetTempFileName();
                latestFrame.ImWrite(tempPath);
                _faceService.CheckPreprocessing(tempPath);
                File.Delete(tempPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Preprocessing test failed:\n{ex.Message}");
            }
        }

    }
}

// Initialize face recognition system
/*
try
{
    string cascadePath = "haarcascade_frontalface_default.xml";

    // Add timeout and error handling for the download
    if (!File.Exists(cascadePath))
    {
        var url = "https://raw.githubusercontent.com/opencv/opencv/master/data/haarcascades/haarcascade_frontalface_default.xml";
        using (var client = new System.Net.WebClient())
        {
            client.DownloadFile(url, cascadePath);
        }

        // Verify the downloaded file
        if (new FileInfo(cascadePath).Length < 1000) // Basic size check
        {
            File.Delete(cascadePath);
            throw new Exception("Downloaded file appears corrupted");
        }
    }


    faceRecognizer = new FaceRecognitionService(cascadePath);
}
catch (Exception ex)
{
    MessageBox.Show($"Failed to initialize face recognition: {ex.Message}");
    // Consider disabling recognition features if initialization fails
}
Here will be the code used to do a camara display and capture image

        buttonStart.Click += buttonStart_Click;
        buttonCapture.Click += buttonCapture_Click;
 
        //private VideoCapture capture;
        //private Mat frame;
        //private Thread cameraThread;
        //private CancellationTokenSource cts;
        //private Mat latestFrame;
    public Mat GetlatestFrameMat()
        {
            return latestFrame?.Clone();//retrive a copy of the image safely
        }
        public Bitmap GetLatestFrameBitmap()
        {
            return latestFrame != null
                ? BitmapConverter.ToBitmap(latestFrame)
                : null;
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            if (capture == null)
            {
                StartCamera();
                buttonStart.Text = "Stop Camera";
            }
            else
            {
                StopCamera();
                buttonStart.Text = "Start Camera";
            }
        }

        private void buttonCapture_Click(object sender, EventArgs e)
        {
            if (capture != null && pictureBox1.Image != null)
            {
                string filename = $"snapshot_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                pictureBox1.Image.Save(filename, System.Drawing.Imaging.ImageFormat.Jpeg);
                MessageBox.Show($"Snapshot saved to:\n{filename}");
            }
        }

        private void StartCamera()
        {
            capture = new VideoCapture(0);
            frame = new Mat();
            cts = new CancellationTokenSource();

            cameraThread = new Thread(() => CaptureCameraLoop(cts.Token))
            {
                IsBackground = true
            };
            cameraThread.Start();
        }


        private void CaptureCameraLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (!capture.IsOpened())
                    break;

                bool ok = capture.Read(frame);
                if (!ok || frame.Empty())
                {
                    Thread.Sleep(50);
                    continue;
                }
                //the valid frame was getted correcly
                latestFrame?.Dispose();
                latestFrame = frame.Clone();//save the latest valid frame

                Bitmap image = BitmapConverter.ToBitmap(frame);
                pictureBox1.Invoke((Action)(() =>
                {
                    pictureBox1.Image?.Dispose();
                    pictureBox1.Image = image;
                }));

                //Call our method to recognize faces
                //RecognizerFacesFromFrame(latestFrame)
            }
        }


        private void StopCamera()
        {
            cts?.Cancel();

            if (cameraThread != null && cameraThread.IsAlive)
            {
                cameraThread.Join(500); // wait up to 500ms
            }

            capture?.Release();
            capture?.Dispose();
            capture = null;

            cts?.Dispose();
            cts = null;
        }
 */