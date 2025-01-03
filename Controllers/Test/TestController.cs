using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
[Authorize]
public class TestController : ControllerBase {

    private readonly LoggingService _loggingService;

    public TestController(LoggingService loggingService) {
        _loggingService = loggingService;
    }

    [HttpGet("hello")]
    public IActionResult GetHello() {
        return Ok("Hello from API!");
    }
}
