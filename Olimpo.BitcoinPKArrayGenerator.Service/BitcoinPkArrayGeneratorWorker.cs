using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Olimpo.RedisProvider;
using StackExchange.Redis;

namespace Olimpo.BitcoinPKArrayGenerator.Service
{
    public class BitcoinPkArrayGeneratorWorker : BackgroundService
    {
        private readonly IDatabaseAsync _databaseAsync;
        private readonly IDatabase _database;

        private const string GroupName = "InitialGroup";
        private const string PublishingStreamChannelName = "BitcoinPkArrayGenerator";

        private readonly IPrivateKeyBytesGenerator _privateKeyBytesGenerator;
        private readonly ILogger<BitcoinPkArrayGeneratorWorker> _logger;
        // private readonly RedisContainer _container;

        public BitcoinPkArrayGeneratorWorker(
            IPrivateKeyBytesGenerator privateKeyBytesGenerator, 
            ILogger<BitcoinPkArrayGeneratorWorker> logger)
        {
            this._privateKeyBytesGenerator = privateKeyBytesGenerator;
            this._logger = logger;

            // [TODO] [AboimPinto]: The ConnectionString should came from the configuration file.
            // var connector = new RedisConnector("127.0.0.1:6379,abortConnect=false");
            // this._container = new RedisContainer(connector, "BitcoinPkArrayGenerator");
            var multiplexer = ConnectionMultiplexer.Connect("127.0.0.1:6379,abortConnect=false");
            this._databaseAsync = multiplexer.GetDatabase(0);
            this._database = multiplexer.GetDatabase(0);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this._logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);

            try
            {
                var couldCreateConsumerGroup = await this._databaseAsync
                    .StreamCreateConsumerGroupAsync(PublishingStreamChannelName, GroupName, StreamPosition.Beginning, true)
                    .ConfigureAwait(false);
            }
            catch
            {
                // user group already created
            }
            

            while(!stoppingToken.IsCancellationRequested)
            {
                var byteArray = this._privateKeyBytesGenerator.GetRandomBytes(32);
                var message = JsonSerializer.Serialize(byteArray);
                await this._databaseAsync.StreamAddAsync(PublishingStreamChannelName, "Message", message);

                this._logger.LogInformation($"PK 32 bits array [{string.Join(", ", byteArray)}]");
                await Task.Delay(1000);
            }
            


            // var streamPublisher = this._container.CreateStreamPublisher<string>(PublishingStreamChannelName);

            // var count = 0;
            // while(!stoppingToken.IsCancellationRequested)
            // {
            //     await streamPublisher
            //         .Publish($"Mensage: {count}")
            //         .ConfigureAwait(false);

            //     this._logger.LogInformation($"Message {count} sent");
            //     count ++;

            //     await Task.Delay(1000);
            // }




            // this._logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);
            // var streamPublisher = this._container.CreateStreamPublisher<byte[]>(PublishingStreamChannelName);

            // while(!stoppingToken.IsCancellationRequested)
            // {
            //     var byteArray = this._privateKeyBytesGenerator.GetRandomBytes(32);                
            //     await streamPublisher
            //         .Publish(byteArray)
            //         .ConfigureAwait(false);

            //     this._logger.LogInformation($"PK 32 bits array [{string.Join(", ", byteArray)}]");
            // }
        }
    }
}
