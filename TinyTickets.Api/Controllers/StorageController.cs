using Microsoft.AspNetCore.Mvc;
using TinyTickets.Api.Services;

namespace TinyTickets.Api.Controllers
{
    [ApiController]
    [Route("storage")]
    public class StorageController : ControllerBase
    {
        private readonly SasTokenService _sas;

        public StorageController(SasTokenService sas)
        {
            _sas = sas;
        }

        [HttpPost("sas-upload")]
        public IActionResult GetUploadSas([FromBody] SasRequest req)
        {
            var sasUrl = _sas.GenerateUploadSas(req.Container, req.FileName);
            return Ok(new { uploadUrl = sasUrl });
        }

        [HttpPost("sas-read")]
        public IActionResult GetReadSas([FromBody] SasRequest req)
        {
            var sasUrl = _sas.GenerateReadSas(req.Container, req.FileName);
            return Ok(new { readUrl = sasUrl });
        }
    }

    public class SasRequest
    {
        public required string Container { get; set; }
        public required string FileName { get; set; }
    }
}
