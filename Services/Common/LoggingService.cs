public class LoggingService {
    private readonly ILogger<LoggingService> _logger;

    public LoggingService(ILogger<LoggingService> logger) {
        _logger = logger;
    }

    public void LogInfo(string message) {
        _logger.LogInformation(message);
    }

    public void LogError(Exception ex) {
        _logger.LogError(ex, "An error occurred.");
    }
}
