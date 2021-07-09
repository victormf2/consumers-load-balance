using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;

namespace BalancedConsumers
{
    public abstract class JsonMessageJob<TMessage> : IJob
    {
        public Task Run(BasicDeliverEventArgs eventArgs)
        {
            var messageJson = Encoding.UTF8.GetString(eventArgs.Body.Span);
            var message = JsonSerializer.Deserialize<TMessage>(messageJson);
            return HandleMessage(message);
        }

        protected abstract Task HandleMessage(TMessage message);
    }
}