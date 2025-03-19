using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;

[ApiController]
[Route("api")]
public class SearchController : ControllerBase
{
    private readonly RpfService _rpfService;

    public SearchController(RpfService rpfService)
    {
        _rpfService = rpfService;
    }

    [HttpGet("search-file")]
    [SwaggerOperation(
    Summary = "Searches for files in the RPF archives",
    Description = "Searches for a file by name in the RPF archive and returns the matching results."
)]
    [SwaggerResponse(200, "Successful search, returns matching file results", typeof(List<string>))]
    [SwaggerResponse(400, "Bad request due to missing filename")]
    [SwaggerResponse(404, "File not found")]
    public IActionResult SearchFile(
    [FromQuery, SwaggerParameter("Filename to search in RPF archive, e.g., filename=prop_alien_egg_01.ydr", Required = true)] string filename)

    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            return BadRequest("Filename is required.");
        }
        var results = _rpfService.SearchFile(filename);
        return results.Count > 0 ? Ok(results) : NotFound($"File '{filename}' not found.");
    }
}
