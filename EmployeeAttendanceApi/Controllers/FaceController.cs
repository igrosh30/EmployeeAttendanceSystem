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
            if (file == null)
                return BadRequest("Please upload a photo.");

            // Convert file → OpenCV image
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            using var image = Cv2.ImDecode(stream.ToArray(), ImreadModes.Color);

            if (image.Empty())
                return BadRequest("Invalid photo.");

            var (name, confidence) = await _svc.RecognizeAsync(image);
            return Ok(new { name, confidence });
        }
    }
}
