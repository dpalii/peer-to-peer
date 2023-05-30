using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Transactions;

class Program
{
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

        Task task = Task.Run(() => StartPeer("Peer1", IPAddress.Parse("127.0.0.1"), port, "127.0.0.1", targetPort));

        task.Wait();
    }

    static async Task StartPeer(string name, IPAddress ipAddress, int port, string remoteIpAddress, int remotePort)
    {
        TcpListener listener = new TcpListener(ipAddress, port);
        listener.Start();
        Console.WriteLine($"{name} listening on {ipAddress}:{port}");

        while (true)
        {
            try
            {
                TcpClient remoteClient = new TcpClient(remoteIpAddress, remotePort);
                Console.WriteLine($"{name} connected to {remoteIpAddress}:{remotePort}");

                Task.Run(() => ProcessIncomingMessages(listener));
                await ProcessOutgoingMessages(remoteClient);
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not connect");
                await Task.Delay(1000);
                continue;
            }
        }
    }

    static async Task ProcessIncomingMessages(TcpListener listener)
    {
        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            Console.WriteLine($"Received connection from {(client.Client.RemoteEndPoint as IPEndPoint).Address}");

            _ = HandleClientCommunicationAsync(client);
        }
    }

    static async Task HandleClientCommunicationAsync(TcpClient client)
    {
        using (StreamReader reader = new StreamReader(client.GetStream()))
        using (StreamWriter writer = new StreamWriter(client.GetStream()))
        {
            string? message;
            while ((message = await reader.ReadLineAsync()) != null)
            {
                Console.WriteLine($"Received message: {message}");

                string response = $"Processed message: {message}";
                await writer.WriteLineAsync(response);
                await writer.FlushAsync();

                Console.WriteLine($"Sent response: {response}");
            }
        }

        client.Close();
    }

    static async Task ProcessOutgoingMessages(TcpClient client)
    {
        using (StreamReader reader = new StreamReader(client.GetStream()))
        using (StreamWriter writer = new StreamWriter(client.GetStream()))
        {
            while (true)
            {
                Console.Write("Enter a message: ");
                string? message = Console.ReadLine();

                await writer.WriteLineAsync(message);
                await writer.FlushAsync();

                string? response = await reader.ReadLineAsync();
                Console.WriteLine($"Received response: {response}");
            }
        }
    }
}
