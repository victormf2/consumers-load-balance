using System.Linq;
using System.Collections.Concurrent;
using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.DependencyInjection;

namespace BalancedConsumers
{
    public class Worker : AsyncDefaultBasicConsumer, IWorker
    {
        private readonly ConcurrentDictionary<ulong, Task> _jobTasks = new();
        public int MaxConcurrentJobs { get; set; }
        public Type JobType { get; set; }
        private readonly IServiceProvider _serviceProvider;

        public Worker(Type jobType, IServiceProvider serviceProvider)
        {
            JobType = jobType;
            _serviceProvider = serviceProvider;
        }

        public override async Task HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, ReadOnlyMemory<byte> body)
        {
            await base.HandleBasicDeliver(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body);

            ClearCompletedTasks();
            while (_jobTasks.Count >= MaxConcurrentJobs)
            {
                await Task.WhenAny(_jobTasks.Values);
                ClearCompletedTasks();
            }

            var bodyBytes = body.ToArray();
            _jobTasks.TryAdd(deliveryTag, Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var job = scope.ServiceProvider.GetService(JobType) as IJob;
                    await job.Run(
                        new BasicDeliverEventArgs(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, bodyBytes));

                    lock (Model)
                    {
                        Model.BasicAck(deliveryTag, multiple: false);
                    }
                }
                catch (Exception ex)
                {
                    HandleException(ex);
                }
            }));

        }

        private void ClearCompletedTasks()
        {
            var completedEntries = _jobTasks.Where(entry => entry.Value.IsCompleted).ToList();
            foreach (var entry in completedEntries)
            {
                _jobTasks.TryRemove(entry);
            }
        }

        protected void HandleException(Exception ex)
        {

        }
    }
}