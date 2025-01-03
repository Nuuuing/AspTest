using System.Net;
using System.Net.Sockets;
using System.Text;

public class UdpServerService : BackgroundService {
    private readonly int _port = 5000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        using var udpServer = new UdpClient(_port);
        var endPoint = new IPEndPoint(IPAddress.Any, 0);

        Console.WriteLine($"UDP Server listening on port {_port}");

        while (!stoppingToken.IsCancellationRequested) {
            try {
                // Receive data with CancellationToken
                UdpReceiveResult result = await udpServer.ReceiveAsync(stoppingToken);

                // Process received data
                var message = Encoding.UTF8.GetString(result.Buffer);
                Console.WriteLine($"Received: {message} from {result.RemoteEndPoint}");

                // Send response
                var response = Encoding.UTF8.GetBytes("Hello from UDP Server");
                await udpServer.SendAsync(response, response.Length, result.RemoteEndPoint);
            }
            catch (OperationCanceledException) {
                Console.WriteLine("Server shutting down...");
                break;
            }
            catch (Exception ex) {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
