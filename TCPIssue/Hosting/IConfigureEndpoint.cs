namespace TCPIssue.Hosting
{
    public interface IConfigureEndpoint
    {
        void Configure(NServiceBus.EndpointConfiguration configuration);
    }
}