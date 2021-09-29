using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Olimpo.RedisProvider;

namespace Olimpo.BitcoinPKArrayGenerator.Service
{
    public class BitcoinPkArrayGeneratorWorker : BackgroundService
    {
        private const string PublishingStreamChannelName = "BitcoinPkArrayGenerator";

        private readonly IPrivateKeyBytesGenerator _privateKeyBytesGenerator;
        private readonly ILogger<BitcoinPkArrayGeneratorWorker> _logger;
        private readonly RedisContainer _container;

        public BitcoinPkArrayGeneratorWorker(
            IPrivateKeyBytesGenerator privateKeyBytesGenerator, 
            ILogger<BitcoinPkArrayGeneratorWorker> logger)
        {
            this._privateKeyBytesGenerator = privateKeyBytesGenerator;
            this._logger = logger;

            // [TODO] [AboimPinto]: The ConnectionString should came from the configuration file.
            var connector = new RedisConnector("127.0.0.1:6379,abortConnect=false");
            this._container = new RedisContainer(connector, "BitcoinPkArrayGeneratorWorker");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this._logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);
            var streamPublisher = this._container.CreateStreamPublisher<byte[]>(PublishingStreamChannelName);

            while(!stoppingToken.IsCancellationRequested)
            {
                var byteArray = this._privateKeyBytesGenerator.GetRandomBytes(32);                
                await streamPublisher
                    .Publish(byteArray)
                    .ConfigureAwait(false);

                this._logger.LogInformation($"PK 32 bits array [{string.Join(", ", byteArray)}]");
            }
        }
    }
}
