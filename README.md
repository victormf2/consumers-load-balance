# RabbitMQ Consumers Load Balance

This works by applying a configurable max number of concurrent running jobs for each queue.

Using the [options pattern of .NET](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-5.0#options-interfaces), we can achieve that configuration at real time.

In this example, `TasksProducer` is responsible for sending new messages to `slow_queue` and `fast_queue` at a hard coded fixed rate.

- `slow_queue` job takes 10 seconds to process each message.
- `fast_queue` job process messages as soon as possible.

By setting `MaxConcurrentJobs` on `appsettings.json` you can fine tune the optimal concurrency for each worker.

On a production scenario, this can be handled by a more convevient [custom configuration provider](https://docs.microsoft.com/en-us/dotnet/core/extensions/custom-configuration-provider) with a [custom change token](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/change-tokens?view=aspnetcore-5.0), so you don't have to ssh on the server to update the configuration. You can find an [example of how you can do this here](https://ofpinewood.com/blog/creating-a-custom-configurationprovider-for-a-entity-framework-core-source).

Feel free to ask any questions in Issues inbox.