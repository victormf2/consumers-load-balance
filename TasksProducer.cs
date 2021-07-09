using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace BalancedConsumers
{
    public class TasksProducer : IHostedService
    {
        private readonly ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private readonly CancellationTokenSource _producerCancellation;
        private readonly IRabbitMQTopology _topology;
        public TasksProducer(IOptions<ConnectionFactory> connectionFactoryOptions, IRabbitMQTopology topology)
        {
            _connectionFactory = connectionFactoryOptions.Value;
            _producerCancellation = new CancellationTokenSource();
            _topology = topology;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _connection = _connectionFactory.CreateConnection();
            _topology.Setup(_connection);
            Task.Run(() => this.ProduceMessagesUntilShutdown());
            return Task.CompletedTask;
        }

        private async Task ProduceMessagesUntilShutdown()
        {
            using var channel = _connection.CreateModel();
            while (!_producerCancellation.IsCancellationRequested)
            {
                PublishMessage(channel, "fast_queue");
                PublishMessage(channel, "slow_queue");
                await Task.Delay(1000);
            }
        }

        private static void PublishMessage(IModel channel, string queue)
        {
            channel.BasicPublish(
                exchange: "",
                routingKey: queue,
                body: Encoding.UTF8.GetBytes("{}")
            );
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _producerCancellation.Cancel();
            return Task.CompletedTask;
        }
    }
}