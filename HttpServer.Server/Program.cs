using System.Formats.Asn1;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace HttpServer.Server;

public sealed class EndpointAttribute
    : Attribute
{
    public string Route { get; init; }

    public EndpointAttribute(string route = "/") =>
        Route = route;
}

public abstract class CustomController
{
    private readonly Dictionary<string, MethodInfo> _endpoints = [];

    public CustomController()
    {
        IEnumerable<MethodInfo> methods = GetType().GetMethods()
            .Where(m => m.GetCustomAttribute<EndpointAttribute>() != null &&
                !m.IsConstructor);

        foreach (var method in methods)
        {
            string route = method.GetCustomAttribute<EndpointAttribute>()!.Route;
            _endpoints.Add(route, method);
        }
    }

    public object? Call(string route, params IEnumerable<ParameterInfo> parameters)
    {
        if (!_endpoints.TryGetValue(route, out MethodInfo? method))
            throw new Exception("Endpoint not found.");

        IEnumerable<ParameterInfo> paramsInfo = method.GetParameters();

        return method.Invoke(this, [.. parameters]);
    }
}


public sealed class HttpServer(string ipAddress, int port)
{
    private readonly TcpListener _server = new(IPAddress.Parse(ipAddress), port);
    private readonly Dictionary<string, CustomController> _controllers = [];

    public async Task Load()
    {
        MapControllers();

        _server.Start();

        while (true)
        {
            using Socket connection = _server.AcceptSocket();
            await HandleRequest(connection);
        }
    }

    private void MapControllers()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        IEnumerable<Type> controllers = assembly.GetTypes()
            .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(CustomController)));

        Console.WriteLine($"Discovered controllers: {controllers.Count()}");
        Console.WriteLine($"Available routes: ");

        foreach (var controller in controllers)
        {
            CustomController instance = (CustomController)Activator
                .CreateInstance(controller)!;

            string route = controller.Name
                .Replace("Controller", string.Empty)
                .ToLower();

            _controllers.Add(route, instance);

            Console.WriteLine(route);
        }
    }

    private async Task HandleRequest(Socket connection)
    {
        using var stream = new NetworkStream(connection);
        using var reader = new StreamReader(stream);

        string request = string.Empty;
        string? line = string.Empty;
        while (!string.IsNullOrEmpty(line = reader.ReadLine())) request += line;

        if (request == string.Empty) throw new Exception("Request's empty");

        string methodType = request.Split(' ')[0];
        string[] route = request.Split(' ')[1].Split('/');

        string controllerRoute = route[1];
        string endpointRoute = string.Empty;
        foreach (string name in route[2 ..])
            endpointRoute += $"/{name}";

        endpointRoute = endpointRoute == string.Empty
            ? "/"  
            : endpointRoute;
        
        Console.WriteLine($"\r\nRequest received!");
        Console.WriteLine($"To Controller: {controllerRoute}");
        Console.WriteLine($"To endpoint: {endpointRoute}");

        if (!_controllers.TryGetValue(controllerRoute, out CustomController? controller))
            throw new Exception("controller not found");


        var result = controller.Call(endpointRoute);
        await WriteResponse(result, stream);
    }

    private static async Task WriteResponse(object? result, NetworkStream stream)
    {
        using var writer = new StreamWriter(stream);
        await writer.WriteLineAsync("HTTP/1.1 200 OK");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync(result?.ToString() ?? "empty");
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