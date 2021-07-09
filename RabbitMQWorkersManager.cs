using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace BalancedConsumers
{
    public class RabbitMQWorkersManager : IHostedService, IDisposable
    {
        private readonly ConnectionFactory _connectionFactory;
        private readonly IServiceProvider _serviceProvider;
        private IConnection _connection;
        private IDisposable _workersConfigurationChangesSubscription;
        private IOptionsMonitor<WorkersConfiguration> _workersConfigurationOptionsMonitor;
        private IDictionary<string, ConsumerConfiguration> _queueConsumers;
        private readonly IRabbitMQTopology _topology;

        public RabbitMQWorkersManager(
            IOptions<ConnectionFactory> connectionFactoryOptions,
            IOptionsMonitor<WorkersConfiguration> workersConfigurationOptionsMonitor,
            IServiceProvider serviceProvider,
            IRabbitMQTopology topology)
        {
            _connectionFactory = connectionFactoryOptions.Value;
            _connectionFactory.DispatchConsumersAsync = true;

            _workersConfigurationOptionsMonitor = workersConfigurationOptionsMonitor;
            _queueConsumers = new Dictionary<string, ConsumerConfiguration>();
            _serviceProvider = serviceProvider;
            _topology = topology;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _connection = _connectionFactory.CreateConnection();
            _topology.Setup(_connection);

            _workersConfigurationChangesSubscription = _workersConfigurationOptionsMonitor.OnChange(UpdateWorkersConfiguration);
            UpdateWorkersConfiguration(_workersConfigurationOptionsMonitor.CurrentValue);
            return Task.CompletedTask;
        }

        private void UpdateWorkersConfiguration(WorkersConfiguration newWorkersConfiguration)
        {
            DropRemovedQueues(newWorkersConfiguration);
            UpdateExistingQueues(newWorkersConfiguration);
            AddNewQueues(newWorkersConfiguration);
        }

        private void DropRemovedQueues(WorkersConfiguration newWorkersConfiguration)
        {
            var queuesToDrop = _queueConsumers.Keys.Where(queueName => !newWorkersConfiguration.QueueWorkerOptionss.ContainsKey(queueName))
                .Concat(newWorkersConfiguration.QueueWorkerOptionss
                    .Where(entry => entry.Value.MaxConcurrentJobs <= 0)
                    .Select(entry => entry.Key))
                .ToList();

            foreach (var queueName in queuesToDrop)
            {
                var consumerConfiguration = _queueConsumers[queueName];
                var channel = consumerConfiguration.Channel;
                channel.BasicCancel(consumerConfiguration.ConsumerTag);
                channel.Close();
                channel.Dispose();
                _queueConsumers.Remove(queueName);
            }
        }

        private void UpdateExistingQueues(WorkersConfiguration newWorkersConfiguration)
        {
            var queuesToUpdate = _queueConsumers.Keys.Where(queueName => newWorkersConfiguration.QueueWorkerOptionss.ContainsKey(queueName));

            foreach (var queueName in queuesToUpdate)
            {
                var worker = _queueConsumers[queueName].Worker;
                worker.MaxConcurrentJobs = newWorkersConfiguration.QueueWorkerOptionss[queueName].MaxConcurrentJobs;
            }
        }

        private void AddNewQueues(WorkersConfiguration newWorkersConfiguration)
        {
            var queuesToAdd = newWorkersConfiguration.QueueWorkerOptionss
                .Where(entry => entry.Value.MaxConcurrentJobs > 0 && !_queueConsumers.ContainsKey(entry.Key))
                .Select(entry => entry.Key);

            foreach (var queueName in queuesToAdd)
            {
                var workerOptions = newWorkersConfiguration.QueueWorkerOptionss[queueName];
                var jobType = workerOptions.JobType;
                if (!jobType.IsAssignableTo(typeof(IJob)))
                {
                    HandleInvalidWorkerType(queueName, workerOptions);
                    continue;
                }

                var channel = _connection.CreateModel();
                var consumer = new Worker(workerOptions.JobType, _serviceProvider)
                {
                    Model = channel,
                    MaxConcurrentJobs = workerOptions.MaxConcurrentJobs,
                };
                // TODO handle possible errors with this operation
                var consumerTag = channel.BasicConsume(consumer, queueName);

                _queueConsumers[queueName] = new ConsumerConfiguration
                {
                    ConsumerTag = consumerTag,
                    Worker = consumer,
                    Channel = channel,
                };
            }
        }

        private void HandleInvalidWorkerType(string queueName, WorkerOptions workerOptions)
        {
            throw new NotImplementedException();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (_connection != null)
            {
                _connection.Close();
            }
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            var innerDisposables = new[]
            {
                _workersConfigurationChangesSubscription,
                _connection
            }.Where(d => d != null);
            foreach (var disposable in innerDisposables)
            {
                disposable.Dispose();
            }
        }
    }

    class ConsumerConfiguration
    {
        public IModel Channel { get; init; }
        public string ConsumerTag { get; init; }
        public IWorker Worker { get; init; }
    }
}