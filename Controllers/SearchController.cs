using Microsoft.AspNetCore.Mvc;
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
    public IActionResult SearchFile([FromQuery] string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            return BadRequest("Filename is required.");
        }
        var results = _rpfService.SearchFile(filename);
        return results.Count > 0 ? Ok(results) : NotFound($"File '{filename}' not found.");
    }
}
