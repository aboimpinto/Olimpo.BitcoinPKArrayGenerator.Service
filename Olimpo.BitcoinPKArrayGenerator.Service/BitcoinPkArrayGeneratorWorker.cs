using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDbQueueService;

namespace Olimpo.BitcoinPKArrayGenerator.Service
{
    public class BitcoinPkArrayGeneratorWorker : BackgroundService
    {
        private readonly IPrivateKeyBytesGenerator _privateKeyBytesGenerator;
        private readonly ILogger<BitcoinPkArrayGeneratorWorker> _logger;
        private IPublisher _publisher;

        public BitcoinPkArrayGeneratorWorker(
            IPrivateKeyBytesGenerator privateKeyBytesGenerator, 
            ILogger<BitcoinPkArrayGeneratorWorker> logger)
        {
            this._privateKeyBytesGenerator = privateKeyBytesGenerator;
            this._logger = logger;

            var isDebug = false;

            #if DEBUG
            isDebug = true;
            #endif

            this._publisher = new Publisher(isDebug);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this._logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);

            while(!stoppingToken.IsCancellationRequested)
            {
                var byteArray = this._privateKeyBytesGenerator.GetRandomBytes(32);

                await this._publisher
                    .SendAsync<PrivateKeyAddress>(new PrivateKeyAddress { PrivateKeyBytes = byteArray })
                    .ConfigureAwait(false);

                this._logger.LogInformation($"PK 32 bits array [{string.Join(", ", byteArray)}]");
            }
        }
    }
}