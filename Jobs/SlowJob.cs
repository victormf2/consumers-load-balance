using System;
using System.Threading.Tasks;

namespace BalancedConsumers.Jobs
{
    public class SlowJobParameters
    {

    }
    public class SlowJob : JsonMessageJob<SlowJobParameters>
    {
        protected override Task HandleMessage(SlowJobParameters message)
        {
            return Task.Delay(TimeSpan.FromSeconds(10));
        }
    }
}