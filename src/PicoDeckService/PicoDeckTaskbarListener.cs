using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicoDeckService
{
    public class PicoDeckTaskbarListener : BackgroundService
    {
        private readonly BackgroundServiceManager _manager;

        public PicoDeckTaskbarListener(BackgroundServiceManager manager)
        {
            _manager = manager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var server = new NamedPipeServerStream("PicoDeckServicePipe", PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                Console.WriteLine("Named pipe server waiting for connection...");
                await server.WaitForConnectionAsync(stoppingToken);

                try
                {
                    Console.WriteLine("Client connected.");
                    await HandleClientAsync(server, stoppingToken);
                }
                catch (IOException e)
                {
                    Console.WriteLine($"Error: {e.Message}");
                }
                finally
                {
                    server.Disconnect();
                }
            }
        }

        private async Task HandleClientAsync(NamedPipeServerStream server, CancellationToken stoppingToken)
        {
            var buffer = new byte[1024];
            while (!stoppingToken.IsCancellationRequested && server.IsConnected)
            {
                var messageBuilder = new StringBuilder();
                do
                {
                    var bytesRead = await server.ReadAsync(buffer, stoppingToken);
                    messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                }
                while (!server.IsMessageComplete);

                var message = messageBuilder.ToString();
                if (string.IsNullOrEmpty(message)) continue;

                Console.WriteLine($"Received: {message}");

                switch (message)
                {
                    case "start":
                        // start the server (or restart if already running)
                        _manager.RequestStart();
                        break;
                    case "stop":
                        _manager.RequestStop();
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
