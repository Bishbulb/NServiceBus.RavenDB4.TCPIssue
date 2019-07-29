namespace NSB.RDB4.Test.Hosting
{
    public interface IConfigureEndpoint
    {
        void Configure(NServiceBus.EndpointConfiguration configuration);
    }
}