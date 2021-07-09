using System;

namespace BalancedConsumers
{
    public interface IWorker
    {
        Type JobType { set; }
        int MaxConcurrentJobs { set; }
    }
}