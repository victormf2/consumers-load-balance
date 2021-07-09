using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BalancedConsumers.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

namespace BalancedConsumers
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var workersConfiguration = hostContext.Configuration.GetSection("Workers");
                    services.Configure<WorkersConfiguration>(workersConfiguration);

                    var rabbitMQConfiguration = hostContext.Configuration.GetSection("RabbitMQ");
                    services.Configure<ConnectionFactory>(rabbitMQConfiguration);

                    services.AddSingleton<IRabbitMQTopology, RabbitMQTopology>();
                    services.AddHostedService<RabbitMQWorkersManager>();

                    services.AddHostedService<TasksProducer>();

                    services.AddWorker<FastJob>("fast_queue");
                    services.AddWorker<SlowJob>("slow_queue");
                });
    }
}
