using HttpServer.Server;

namespace HttpServer.Server.Controllers;

public sealed class HelloController
    : CustomController
{

    [Endpoint]
    public string Hello() => "Hello, world!";
}