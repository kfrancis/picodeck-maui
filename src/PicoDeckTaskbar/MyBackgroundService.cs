using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicoDeckTaskbar
{
    public class BackgroundServiceManager
    {
        public Action StartRequested { get; set; }
        public Action StopRequested { get; set; }

        public void RequestStart() => StartRequested?.Invoke();

        public void RequestStop() => StopRequested?.Invoke();
    }

    public abstract class MyBackgroundService
    {
        private CancellationTokenSource? _cts;
        private Task? _executingTask;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts ??= CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Store the task we're executing
            _executingTask = ExecuteAsync(_cts.Token);

            // If the task is completed then return it, otherwise it's running
            return _executingTask.IsCompleted ? _executingTask : Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_executingTask == null)
            {
                return;
            }

            // Signal cancellation to the executing method
            _cts?.Cancel();

            // Wait until the task completes or the stop token triggers
            await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));

            // Throw if cancellation triggered
            cancellationToken.ThrowIfCancellationRequested();
        }

        protected abstract Task ExecuteAsync(CancellationToken stoppingToken);
    }
}
