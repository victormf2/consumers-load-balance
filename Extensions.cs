using System;
using System.Collections.Generic;
using BalancedConsumers;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class Extensions
    {
        public static void AddWorker<TJobType>(this IServiceCollection services, string messageQueue)
            where TJobType : IJob =>
            AddWorker(services, typeof(TJobType), messageQueue);

        public static void AddWorker(this IServiceCollection services, Type jobType, string messageQueue)
        {
            services.AddScoped(jobType);
            services.Configure<WorkersConfiguration>(workersConfiguration =>
            {
                workersConfiguration.QueueWorkerOptionss.TryAdd(messageQueue, new WorkerOptions());
                workersConfiguration.QueueWorkerOptionss[messageQueue].JobType = jobType;
            });
        }
        
    }
}