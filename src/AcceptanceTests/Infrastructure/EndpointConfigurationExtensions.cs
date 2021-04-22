namespace NServiceBus.Encryption.MessageProperty.AcceptanceTests
{
    using Configuration.AdvancedExtensibility;
    using NServiceBus.Transport;

    public static class EndpointConfigurationExtensions
    {
        public static TransportDefinition ConfigureTransport(this EndpointConfiguration endpointConfiguration) =>
            endpointConfiguration.GetSettings().Get<TransportDefinition>();

        public static RoutingSettings ConfigureRouting(this EndpointConfiguration configuration) =>
            new RoutingSettings(configuration.GetSettings());
    }
}