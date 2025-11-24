using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LeeTeke.HttpServerLite.Hosting
{
    internal class HttpServerListHostedService : IHostedService
    {

        private readonly Action _startCallback;
        private readonly Action _stopCallback;
        public HttpServerListHostedService(Action startCallback, Action stopCallback)
        {
            _startCallback = startCallback;
            _stopCallback = stopCallback;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            _startCallback.Invoke();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            _stopCallback.Invoke();
        }
    }
}
