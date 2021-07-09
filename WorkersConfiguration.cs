using System;
using System.Collections.Generic;

namespace BalancedConsumers
{
    public class WorkersConfiguration
    {
        public IDictionary<string, WorkerOptions> QueueWorkerOptionss { get; set; }
    }

    public class WorkerOptions
    {
        public Type JobType { get; set; }
        public int MaxConcurrentJobs { get; set; } = 1;
    }
}
