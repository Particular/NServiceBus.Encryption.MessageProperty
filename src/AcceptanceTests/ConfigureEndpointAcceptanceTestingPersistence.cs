using NServiceBus.AcceptanceTesting.Support;
using NServiceBus;
using System.Threading.Tasks;

public class ConfigureEndpointAcceptanceTestingPersistence : IConfigureEndpointTestExecution
{
    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        configuration.UsePersistence<AcceptanceTestingPersistence>();
        return Task.CompletedTask;
    }

    public Task Cleanup()
    {
        // Nothing required for in-memory persistence
        return Task.CompletedTask;
    }
}