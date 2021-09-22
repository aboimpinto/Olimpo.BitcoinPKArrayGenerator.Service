using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Olimpo.BitcoinPKArrayGenerator.Service
{
    public class BitcoinPkArrayGeneratorWorker : BackgroundService
    {
        private readonly ILogger<BitcoinPkArrayGeneratorWorker> _logger;

        public BitcoinPkArrayGeneratorWorker(ILogger<BitcoinPkArrayGeneratorWorker> logger)
        {
            this._logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this._logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);
            while(!stoppingToken.IsCancellationRequested)
            {
                this._logger.LogInformation("Generate 32 bits array at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000);
            }
        }
    }
}
