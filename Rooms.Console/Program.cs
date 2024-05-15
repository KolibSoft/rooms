﻿
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using KolibSoft.Rooms.Core.Protocol;
using KolibSoft.Rooms.Core.Services;
using KolibSoft.Rooms.Core.Streams;

var mode = Console.ReadLine();
switch (mode)
{
    case "TcpServer": await RunTcpServer(); break;
    case "WebServer": await RunWebServer(); break;
    case "TcpClient": await RunTcpClient(); break;
    case "WebClient": await RunWebClient(); break;
}

async Task RunTcpServer()
{
    using var service = new RoomServer() { Logger = Console.Error };
    service.Start();
    using var listener = new TcpListener(IPAddress.Any, 55000);
    await ListenAsync(listener);
    //////////////////////////////////////////////////////////////
    async Task ListenAsync(TcpListener listener)
    {
        listener.Start();
        while (service.IsRunning)
            try
            {
                var client = await listener.AcceptTcpClientAsync();
                var stream = new RoomNetworkStream(client);
                _ = service.ListenAsync(stream);
            }
            catch (Exception error)
            {
                await Console.Error.WriteLineAsync($"Error listening connection: {error}");
            }
        listener.Stop();
    }
}

async Task RunWebServer()
{
    using var service = new RoomServer() { Logger = Console.Error };
    service.Start();
    using var listener = new HttpListener();
    listener.Prefixes.Add("http://localhost:55000/");
    await ListenAsync(listener);
    ///////////////////////////////////////////////////////////
    async Task ListenAsync(HttpListener listener)
    {
        listener.Start();
        while (service.IsRunning)
            try
            {
                var httpContext = await listener.GetContextAsync();
                var wsContext = await httpContext.AcceptWebSocketAsync(null);
                var socket = wsContext.WebSocket;
                var stream = new RoomWebStream(socket);
                _ = service.ListenAsync(stream);
            }
            catch (Exception error)
            {
                await Console.Error.WriteLineAsync($"Error listening connection: {error}");
            }
        listener.Stop();
    }
}

async Task RunTcpClient()
{
    using var service = new RoomClient() { Logger = Console.Error };
    service.Start();
    using var client = new TcpClient();
    await client.ConnectAsync(IPAddress.Loopback, 55000);
    using var stream = new RoomNetworkStream(client);
    CommandAsync();
    await service.ListenAsync(stream);
    ///////////////////////////////////////////////////////
    async void CommandAsync()
    {
        while (service.IsRunning)
        {
            var command = await Task.Run(() => Console.ReadLine());
            try
            {
                var parts = command!.Split(" ");
                var message = new RoomMessage
                {
                    Verb = parts[0],
                    Channel = int.Parse(parts[1]),
                    Content = new MemoryStream(Encoding.UTF8.GetBytes(string.Join(' ', parts.AsSpan().Slice(2).ToArray())))
                };
                await service.SendAsync(message);
            }
            catch (Exception error)
            {
                await Console.Error.WriteLineAsync($"Error parsing command: {error}");
            }
            await Task.Delay(100);
        }
    }
}

async Task RunWebClient()
{
    using var service = new RoomClient() { Logger = Console.Error };
    service.Start();
    using var client = new ClientWebSocket();
    await client.ConnectAsync(new Uri("ws://localhost:55000"), default);
    using var stream = new RoomWebStream(client);
    CommandAsync();
    await service.ListenAsync(stream);
    //////////////////////////////////////////////////////////////////////
    async void CommandAsync()
    {
        while (service.IsRunning)
        {
            var command = await Task.Run(() => Console.ReadLine());
            try
            {
                var parts = command!.Split(" ");
                var message = new RoomMessage
                {
                    Verb = parts[0],
                    Channel = int.Parse(parts[1]),
                    Content = new MemoryStream(Encoding.UTF8.GetBytes(string.Join(' ', parts.AsSpan().Slice(2).ToArray())))
                };
                await service.SendAsync(message);
            }
            catch (Exception error)
            {
                await Console.Error.WriteLineAsync($"Error parsing command: {error}");
            }
            await Task.Delay(100);
        }
    }
}

class RoomClient : RoomService
{

    protected override async ValueTask OnReceiveAsync(IRoomStream stream, RoomMessage message, CancellationToken token)
    {
        var clone = new MemoryStream((int)message.Content.Length);
        await message.Content.CopyToAsync(clone);
        Console.WriteLine($"< {message.Verb} {message.Channel} {Encoding.UTF8.GetString(clone.ToArray())}");
    }

    protected override void OnStart()
    {
        base.OnStart();
        Console.WriteLine($"Service Started");
    }

    protected override void OnStop()
    {
        base.OnStop();
        Console.WriteLine($"Service Stopped");
    }

    public RoomClient(RoomServiceOptions? options = null) : base(options) { }

}

class RoomServer : RoomHub
{

    protected override async ValueTask OnReceiveAsync(IRoomStream stream, RoomMessage message, CancellationToken token)
    {
        var clone = new MemoryStream((int)message.Content.Length);
        await message.Content.CopyToAsync(clone);
        Console.WriteLine($"< {message.Verb} {message.Channel} {Encoding.UTF8.GetString(clone.ToArray())}");
        message.Content.Seek(0, SeekOrigin.Begin);
        await base.OnReceiveAsync(stream, message, token);
    }

    protected override void OnStart()
    {
        base.OnStart();
        Console.WriteLine($"Service Started");
    }

    protected override void OnStop()
    {
        base.OnStop();
        Console.WriteLine($"Service Stopped");
    }

    public RoomServer(RoomServiceOptions? options = null) : base(options) { }

}