using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace PicoDeckTaskbar
{
    internal class PicoDeckServer : MyBackgroundService
    {
        private readonly TcpListener _listener;
        private readonly BackgroundServiceManager _manager;
        private CancellationTokenSource _cancellationTokenSource;

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

        private void HandleStartRequested()
        {
        }

        private void HandleStopRequested()
        {
            if (_cancellationTokenSource == null) return;

            _cancellationTokenSource.Cancel();
        }
        private object ProcessClientAsync(TcpClient client, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
