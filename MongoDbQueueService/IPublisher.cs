using System.Threading.Tasks;


namespace MongoDbQueueService
{
    public interface IPublisher
    {
        Task SendAsync<T>(T payload, int priority = 0);
    }
}
