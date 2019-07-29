using System;

namespace NSB.RDB4.Test.Hosting
{
    public class ActionConfigureEndpoint : IConfigureEndpoint
    {
        private readonly Action<NServiceBus.EndpointConfiguration> action;

        public ActionConfigureEndpoint(Action<NServiceBus.EndpointConfiguration> action)
        {
            this.action = action;
        }

        public void Configure(NServiceBus.EndpointConfiguration configuration)
        {
            action(configuration);
        }
    }
}