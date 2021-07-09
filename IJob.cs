using System.Threading.Tasks;
using RabbitMQ.Client.Events;

namespace BalancedConsumers
{
    public interface IJob
    {
        Task Run(BasicDeliverEventArgs eventArgs);
    }
}