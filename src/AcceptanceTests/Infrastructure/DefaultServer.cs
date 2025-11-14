namespace NServiceBus.Encryption.MessageProperty.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting.Support;
    using Configuration.AdvancedExtensibility;
    using Microsoft.Extensions.DependencyInjection;

    public class DefaultServer : IEndpointSetupTemplate
    {
        public async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, Func<EndpointConfiguration, Task> configurationBuilderCustomization)
        {
            var configuration = new EndpointConfiguration(endpointConfiguration.EndpointName);
            configuration.UseSerialization<SystemJsonSerializer>();

            configuration.EnableInstallers();

            var recoverability = configuration.Recoverability();
            recoverability.Delayed(delayed => delayed.NumberOfRetries(0));
            recoverability.Immediate(immediate => immediate.NumberOfRetries(0));

            configuration.UseTransport(new LearningTransport());

            configuration.RegisterComponents(r =>
            {
                var type = runDescriptor.ScenarioContext.GetType();
                while (type != typeof(object))
                {
                    r.AddSingleton(type, runDescriptor.ScenarioContext);
                    type = type.BaseType;
                }
            });

            configuration.UsePersistence<AcceptanceTestingPersistence>();

            configuration.GetSettings().SetDefault("ScaleOut.UseSingleBrokerQueue", true);
            await configurationBuilderCustomization(configuration);

            configuration.ScanTypesForTest(endpointConfiguration);

            return configuration;
        }
    }
}
