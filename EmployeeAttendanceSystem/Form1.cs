using OpenCvSharp;
using OpenCvSharp.Extensions;

/*
 Person0 - Courtous
 Person1 - Dybala
 Person2 - Kross
 Person3 - Pogba
 */


namespace EmployeeAttendanceSystem
{
    public partial class Form1 : Form
    {
        private Mat latestFrame;
        private FaceRecognitionService _faceService;
        private const string dataSetPath = "C:\\Users\\Igor\\source\\repos\\EmployeeAttendanceSystem\\EmployeeAttendanceSystem\\FacesDataset";
        private const string modelPath = @"C:\Users\Igor\source\repos\EmployeeAttendanceSystem\EmployeeAttendanceSystem\Models\faceNet.onnx";
        private string selectedImagePath;

        public Form1()
        {
            InitializeComponent();
            InitializeFaceRecognitionService();
        }

        private void InitializeFaceRecognitionService()
        {
            //initialize the model&dataset
            try
            {
                if (!File.Exists(modelPath))
                {
                    MessageBox.Show($"Model file not found at:\n{modelPath}\n\nPlease check the file path.",
                                  "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _faceService = new FaceRecognitionService(modelPath);

                // Load your dataset
                if (!Directory.Exists(dataSetPath))
                {
                    MessageBox.Show($"Dataset directory not found:\n{dataSetPath}",
                                  "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing face recognition: {ex.Message}",
                              "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    if (_faceService == null)
                    {
                        MessageBox.Show("Face Recognition service is not initialized");
                        return;
                    }
                    selectedImagePath = openFileDialog1.FileName;
                    pictureBox1.Image?.Dispose();
                    latestFrame?.Dispose();

                    pictureBox1.Image = new Bitmap(selectedImagePath);
                    latestFrame = BitmapConverter.ToMat((Bitmap)pictureBox1.Image);
                    
                    if(!_faceService.HasEmbeddings())
                    {
                        MessageBox.Show("No face embeddings found. Please train the model first.");
                        return;
                    }

                    labelUpload.Text = "Image loaded successfully. Strarting the Recognition Process.";
                    
                    var (predictedName, predictedConfidence) = _faceService.Recognize(latestFrame);
                    
                    MessageBox.Show($"Person recognized: {predictedName}\nConfidence: {predictedConfidence:F2}%",
                             "Recognition Result",
                             MessageBoxButtons.OK,
                             MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}");
                    labelUpload.Text = "Error loading image.";
                }
            }
            
        }

        
        private void buttonTrain_Click(object sender, EventArgs e)
        {
            buttonTrain.Enabled = false;
            labelUpload.Text = "Creating the embeedings...";
            
            _faceService.RegisterFacesFromDataset(dataSetPath);

            Application.DoEvents(); // Force UI update
            if (_faceService.HasEmbeddings())
            {
                labelUpload.Text = "Embeedings done";
            }
            else
            {
                labelUpload.Text = "Failed Creating the Embeedings";
            }
            
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
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