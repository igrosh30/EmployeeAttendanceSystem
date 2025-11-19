using EmployeeAttendanceApi.Services;
using Microsoft.AspNetCore.Mvc;
using OpenCvSharp;

namespace EmployeeAttendanceApi.Controllers
{
    [ApiController]
    [Route("api/face")]

    public class FaceController : ControllerBase
    {

        private readonly FaceRecognitionService _svc;

        public FaceController(FaceRecognitionService svc)
        {
            _svc = svc;
        }

        [HttpPost("train")]
        public async Task<IActionResult> Train([FromQuery] string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return BadRequest("Please send a folder path.");

            try
            {
                await _svc.RegisterAsync(path);
                return Ok("Training done! System knows everyone.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // RECOGNIZE: Check who is in the photo
        [HttpPost("recognize")]
        public async Task<IActionResult> Recognize(IFormFile file)
        {
            if (file == null) return BadRequest("Please upload a photo.");
            
            byte[] originalImageBytes;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                originalImageBytes = memoryStream.ToArray();   // ← 100% original JPEG/PNG from user
            }

            using var image = Cv2.ImDecode(originalImageBytes, ImreadModes.Color);
            if (image.Empty()) return BadRequest("Invalid photo.");

            double confidenceThreshold = 60.0;
            var (name, confidence) = await _svc.RecognizeAsync(image);

            bool isRecognize = confidence >= confidenceThreshold && !string.IsNullOrEmpty(name);
            if(isRecognize)
            {
                _svc.SaveImage2Folder(name, originalImageBytes);
            }
            //should I save here? or inside the RecognizeAsync?
            return Ok(new { name, confidence });
        }
    }
}
