using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Transactions;

public class Peer
{
    private ITcpClient _tcpClient;
    private ITcpListener _listener;
    private IConsole _console;
    public Peer(ITcpClient remoteClient, ITcpListener listener, IConsole console)
    {
        _console = console;
        _tcpClient = remoteClient;
        _listener = listener;
    }
    static void Main()
    {
        Console.WriteLine("Enter your port:");
        string? input = Console.ReadLine();
        if (input == null)
        {
            return;
        }
        int port = int.Parse(input);

        Console.WriteLine("Enter target port:");
        input = Console.ReadLine();
        if (input == null)
        {
            return;
        }
        int targetPort = int.Parse(input);

        IConsole console = new ConsoleWrapper();
        TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
        listener.Start();
        while (true)
        {
            try {
                TcpClient client = new TcpClient("127.0.0.1", targetPort);

                Peer peer = new Peer(new TcpClientWrapper(client), new TcpListenerWrapper(listener), console);
                Task.Run(() => peer.StartPeer()).Wait();
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not connect: " + ex.Message);
                Task.Delay(1000).Wait();
                continue;
            }
        }
    }


    public async Task StartPeer()
    {
        Task.Run(() => ProcessIncomingMessages());
        await ProcessOutgoingMessages();
    }

    public async Task ProcessIncomingMessages()
    {
        ITcpClient client = await _listener.AcceptTcpClientAsync();
        _console.WriteLine($"Received connection from {client.GetAddress()}");

        _ = HandleClientCommunicationAsync(client);
    }

    private async Task HandleClientCommunicationAsync(ITcpClient client)
    {
        using (StreamReader reader = new StreamReader(client.GetStream()))
        using (StreamWriter writer = new StreamWriter(client.GetStream()))
        {
            string? message;
            while ((message = await reader.ReadLineAsync()) != null)
            {
                _console.WriteLine($"Received message: {message}");

                string response = $"Processed message: {message}";
                await writer.WriteLineAsync(response);
                await writer.FlushAsync();

                _console.WriteLine($"Sent response: {response}");
            }
        }

        _tcpClient.Close();
    }

    public async Task ProcessOutgoingMessages()
    {
        using (StreamWriter writer = new StreamWriter(_tcpClient.GetStream()))
        using (StreamReader reader = new StreamReader(_tcpClient.GetStream()))
        {
            while (true)
            {
                _console.WriteLine("Enter a message: ");
                string? message = _console.ReadLine();
                if (message == null || message == "")
                {
                    break;
                }

                await writer.WriteLineAsync(message);
                await writer.FlushAsync();
                string? response = await reader.ReadLineAsync();
                _console.WriteLine($"Received response: {response}");
            }
        }
    }
}


public class TcpListenerWrapper : ITcpListener
{
    private readonly TcpListener _listener;

    public TcpListenerWrapper(TcpListener listener)
    {
        _listener = listener;
    }

    public async Task<ITcpClient> AcceptTcpClientAsync()
    {
        TcpClient client = await _listener.AcceptTcpClientAsync();

        return new TcpClientWrapper(client);
    }
}

public interface ITcpListener
{
    public Task<ITcpClient> AcceptTcpClientAsync();
}

public class TcpClientWrapper : ITcpClient
{
    private readonly TcpClient _client;

    public TcpClientWrapper(TcpClient client)
    {
        _client = client;
    }

    public string GetAddress()
    {
        return (_client.Client.RemoteEndPoint as IPEndPoint).Address.ToString();
    }

    public void Close()
    {
        _client.Close();
    }

    public Stream GetStream()
    {
        return _client.GetStream();
    }
}

public interface ITcpClient
{
    public void Close();
    public Stream GetStream();
    public string GetAddress();
}

public class ConsoleWrapper: IConsole
{
    public string? ReadLine()
    {
        return Console.ReadLine();
    }

    public void WriteLine(string line)
    {
        Console.WriteLine(line);
    }
}

public interface IConsole
{
    public string? ReadLine();
    public void WriteLine(string value);
}
