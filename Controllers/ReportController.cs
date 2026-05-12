using DbmsComparison.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DbmsComparison.Api.Controllers;

[ApiController]
[Route("api/report")]
public class ReportController(ReportService reportService) : ControllerBase
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
}
