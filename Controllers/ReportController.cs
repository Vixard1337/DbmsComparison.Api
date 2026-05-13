using DbmsComparison.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DbmsComparison.Api.Controllers;

[ApiController]
[Route("api/report")]
public class ReportController(ReportService reportService, DiagramService diagramService) : ControllerBase
{
    [HttpGet("summary")]
    public IActionResult Summary()
    {
        return Ok(reportService.BuildSummary());
    }

    [HttpGet("plots")]
    public IActionResult Plots()
    {
        return Ok(new { Files = reportService.GeneratePlots() });
    }

    [HttpGet("pdf")]
    public IActionResult Pdf()
    {
        var path = reportService.GeneratePdfReport();
        return Ok(new { File = path });
    }

    [HttpGet("files/{fileName}")]
    public IActionResult Download(string fileName)
    {
        var resultsDirectory = Path.Combine(AppContext.BaseDirectory, "results");
        var path = Path.Combine(resultsDirectory, fileName);
        if (!System.IO.File.Exists(path))
        {
            return NotFound(new { message = "File not found." });
        }

        return PhysicalFile(path, "application/octet-stream", fileName);
    }

    [HttpPost("diagrams")]
    public IActionResult Diagrams()
    {
        var files = diagramService.GenerateDiagrams();
        return Ok(new { Files = files });
    }
}
