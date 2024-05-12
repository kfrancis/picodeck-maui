using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using Input;

namespace PicoDeckService
{
    public class BackgroundServiceManager
    {
        public Action? StartRequested { get; set; }
        public Action? StopRequested { get; set; }

        public void RequestStart() => StartRequested?.Invoke();

        public void RequestStop() => StopRequested?.Invoke();
    }

    public class PicoDeckServer : BackgroundService
    {
        private const int BroadcastPort = 9999;
        private const int CommunicationPort = 10000; // Port for normal operation
        private readonly CancellationTokenSource _broadcastCts = new();
        private readonly BackgroundServiceManager _manager;
        private Thread? _broadcastThread;
        private CancellationTokenSource? _cancellationTokenSource;
        private TcpListener? _listener;

        public PicoDeckServer(BackgroundServiceManager manager)
        {
            _manager = manager;
            _manager.StartRequested += HandleStartRequested;
            _manager.StopRequested += HandleStopRequested;
        }

        public void BroadcastPresence()
        {
            using var serverSocket = new UdpClient();
            serverSocket.EnableBroadcast = true;
            var endpoint = new IPEndPoint(IPAddress.Broadcast, BroadcastPort);
            var message = Encoding.UTF8.GetBytes("Server Here");

            Console.WriteLine("Starting client discovery ...");
            while (!_broadcastCts.Token.IsCancellationRequested)
            {
                serverSocket.Send(message, message.Length, endpoint);
                Console.WriteLine("Broadcasted server presence, waiting for a client ..");
                Thread.Sleep(5000); // Broadcast every 5 seconds
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

            // Start broadcasting in a separate thread
            _broadcastThread = new Thread(BroadcastPresence);
            _broadcastThread.Start();

            // Listen for client acknowledgment
            var listener = new UdpClient(CommunicationPort);
            IPEndPoint? clientEndpoint = null;
            while (!_broadcastCts.IsCancellationRequested)
            {
                var receivedData = listener.Receive(ref clientEndpoint);
                Console.WriteLine("Received acknowledgment from client.");
                await _broadcastCts.CancelAsync(); // Stop broadcasting
            }

            // Configure the TCP listener to listen on any IP address
            _listener = new TcpListener(IPAddress.Any, CommunicationPort);
            _listener.Start();
            Console.WriteLine("TCP server started. Listening for connections ...");

            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (_listener.Pending())
                {
                    var client = await _listener.AcceptTcpClientAsync(_cancellationTokenSource.Token);
                    Console.WriteLine("Client connected. Processing...");
                    // Handle the client connection in a separate task
                    _ = ProcessClientAsync(client, _cancellationTokenSource.Token);
                }
                else
                {
                    await Task.Delay(1000, _cancellationTokenSource.Token); // Wait a bit if no connection is pending
                }
            }

            _manager.StopRequested -= HandleStopRequested;
            _manager.StartRequested -= HandleStartRequested;
            _listener.Stop();
        }

        private static async Task ProcessClientAsync(TcpClient client, CancellationToken stoppingToken)
        {
            try
            {
                using (client)
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    var jsonString = await reader.ReadLineAsync(stoppingToken);
                    Console.WriteLine($"Received: {jsonString}");

                    if (!string.IsNullOrEmpty(jsonString))
                    {
                        try
                        {
                            // Deserialize the JSON string into a Shortcut object
                            var shortcut = ShortcutAction.FromJson(jsonString);
                            if (shortcut != null && shortcut.Keys != null)
                            {
                                Console.WriteLine($"Executing shortcut for {shortcut.Application}: {shortcut.ShortcutName}");

                                // Use InputSimulator to simulate the keyboard shortcut
                                var inputSimulator = Inputs.Use<IKeyboardSimulation>();
                                var keys = new List<InputKeys>();
                                foreach (var key in shortcut.Keys)
                                {
                                    // Convert string representation of key to VirtualKeyCode
                                    if (Enum.TryParse<InputKeys>(key, out var virtualKeyCode))
                                    {
                                        keys.Add(virtualKeyCode);
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Unknown key: {key}");
                                    }
                                }

                                if (keys.Count != 0)
                                {
                                    inputSimulator.KeyClick([.. keys]);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing message: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing client: {ex.Message}");
            }
        }

        private void HandleStartRequested()
        {
        }

        private void HandleStopRequested()
        {
            _broadcastCts.Cancel();
            _cancellationTokenSource?.Cancel();
        }

        public record Shortcut(string Application, string ShortcutName, string[] Keys);
    }
}
