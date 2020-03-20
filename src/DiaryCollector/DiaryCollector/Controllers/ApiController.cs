using DiaryCollector.InputModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace DiaryCollector.Controllers {
    
    [Route("api")]
    public class ApiController : ControllerBase {

        private readonly ILogger<ApiController> _logger;

        public ApiController(
            ILogger<ApiController> logger
        ) {
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(
            [FromBody] DailyStats stats
        ) {
            _logger.LogInformation("Receiving daily stats from device {0}", stats.DeviceId);


            return Ok(stats);
        }

    }

}
