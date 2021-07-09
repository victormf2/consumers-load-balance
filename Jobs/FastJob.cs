using System.Threading.Tasks;

namespace BalancedConsumers.Jobs
{
    public class FastJobParameters
    {

    }
    public class FastJob : JsonMessageJob<FastJobParameters>
    {
        protected override Task HandleMessage(FastJobParameters message)
        {
            return Task.CompletedTask;
        }
    }
}