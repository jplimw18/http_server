using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace HttpServer.Server;

public sealed class HttpServer(string ipAddress, int port)
{
    private readonly TcpListener _server = new(IPAddress.Parse(ipAddress), port);

    public async Task Load()
    {
        _server.Start();
        
        while (true)
        {
            using Socket connection = _server.AcceptSocket();

            using var stream = new NetworkStream(connection);
            using var read = new StreamReader(stream);
            
            string? line = string.Empty;
            while (!string.IsNullOrEmpty(line = read.ReadLine())) Console.WriteLine(line);

            string body = "<html><body><h1>Hello from C# Server</h1></body></html>";
            using var write = new StreamWriter(stream);
            await write.WriteLineAsync("HTTP/1.1 200 OK");
            await write.WriteLineAsync("Content-Type: text/html");
            await write.WriteLineAsync($"Content-Length: {Encoding.UTF8.GetByteCount(body)}");
            await write.WriteLineAsync();
            await write.WriteLineAsync(body);
        }
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        HttpServer server = new("127.0.0.1", 3000);
        await server.Load();
    }
}