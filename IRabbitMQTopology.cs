using RabbitMQ.Client;

namespace BalancedConsumers
{
    public interface IRabbitMQTopology
    {
        void Setup(IConnection connection);
    }

    public class RabbitMQTopology : IRabbitMQTopology
    {
        public void Setup(IConnection connection)
        {
            using var channel = connection.CreateModel();

            DefaultQueueDeclare(channel, "fast_queue");
            DefaultQueueDeclare(channel, "slow_queue");

            channel.Close();
        }
        private static void DefaultQueueDeclare(IModel channel, string queueName)
        {
            channel.QueueDeclare(
                queue: queueName, 
                durable: true,
                exclusive: false,
                autoDelete: false);
        }
    }
}