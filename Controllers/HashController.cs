using CodeWalker.GameFiles;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CodeWalker.API.Controllers
{
    [ApiController]
    [Route("api/hash")]
    public class HashController : ControllerBase
    {
        [HttpGet("jenkins")]
        [SwaggerOperation(
            Summary = "Generate Jenkins hash",
            Description = "Generates a Jenkins hash from the input string and returns its uint, int, and hex representations."
        )]
        [SwaggerResponse(200, "Jenkins hash generated", typeof(JenkHash))]
        [SwaggerResponse(400, "Missing or invalid input")]
        public IActionResult GetJenkinsHash(
            [FromQuery, SwaggerParameter("The string to hash", Required = true)] string input,
            [FromQuery, SwaggerParameter("Encoding to use: UTF8 (0) or ASCII (1)")] JenkHashInputEncoding encoding = JenkHashInputEncoding.UTF8
        )
        {
            if (string.IsNullOrWhiteSpace(input))
                return BadRequest("Missing input string.");

            var result = new JenkHash(input, encoding);
            return Ok(new
            {
                input = result.Text,
                encoding = result.Encoding.ToString(),
                hashUint = result.HashUint,
                hashInt = result.HashInt,
                hashHex = result.HashHex
            });
        }
    }
}
