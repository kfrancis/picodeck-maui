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
        private const int BroadcastPort = 9999;
        private const int CommunicationPort = 10000; // Port for normal operation
        private readonly TcpListener _listener;
        private readonly BackgroundServiceManager _manager;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationTokenSource _broadcastCts = new();
        private Thread _broadcastThread;

        public PicoDeckServer(BackgroundServiceManager manager)
        {
            // Configure the TCP listener to listen on any IP address
            _listener = new TcpListener(IPAddress.Any, CommunicationPort);
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
                    var client = await _listener.AcceptTcpClientAsync(_cancellationTokenSource.Token);

                    await _broadcastCts.CancelAsync();

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

        public void BroadcastPresence()
        {
            using var serverSocket = new UdpClient();
            serverSocket.EnableBroadcast = true;
            var endpoint = new IPEndPoint(IPAddress.Broadcast, BroadcastPort);
            var message = Encoding.UTF8.GetBytes("Server Here");

            Console.WriteLine("Starting broadcast...");
            while (!_broadcastCts.Token.IsCancellationRequested)
            {
                serverSocket.Send(message, message.Length, endpoint);
                Console.WriteLine("Broadcasted server presence");
                Thread.Sleep(10000); // Broadcast every 10 seconds
            }
        }

        private void HandleStartRequested()
        {
            // Start broadcasting in a separate thread
            _broadcastThread = new Thread(BroadcastPresence);
            _broadcastThread.Start();
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
