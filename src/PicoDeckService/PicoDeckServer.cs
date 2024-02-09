using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;
using System.Text.Json;

namespace PicoDeckService
{
    public class BackgroundServiceManager
    {
        public Action StartRequested { get; set; }
        public Action StopRequested { get; set; }

        public void RequestStart() => StartRequested?.Invoke();

        public void RequestStop() => StopRequested?.Invoke();
    }

    public class PicoDeckServer : BackgroundService
    {
        private readonly TcpListener _listener;
        private readonly BackgroundServiceManager _manager;
        private CancellationTokenSource? _cancellationTokenSource;

        public PicoDeckServer(BackgroundServiceManager manager)
        {
            // Configure the TCP listener to listen on any IP address, port 12345
            _listener = new TcpListener(IPAddress.Any, 12345);
            _manager = manager;
            _manager.StartRequested += HandleStartRequested;
            _manager.StopRequested += HandleStopRequested;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            _listener.Start();
            Console.WriteLine("TCP Server started. Listening for connections...");

            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (_listener.Pending())
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync(_cancellationTokenSource.Token);
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
                using (NetworkStream stream = client.GetStream())
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
                                var inputSimulator = new InputSimulator();
                                foreach (var key in shortcut.Keys)
                                {
                                    // Convert string representation of key to VirtualKeyCode
                                    if (Enum.TryParse<VirtualKeyCode>(key, out var virtualKeyCode))
                                    {
                                        inputSimulator.Keyboard.KeyPress(virtualKeyCode);
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Unknown key: {key}");
                                    }
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
            if (_cancellationTokenSource == null) return;

            _cancellationTokenSource.Cancel();
        }

        public record Shortcut(string Application, string ShortcutName, string[] Keys);
    }
}
