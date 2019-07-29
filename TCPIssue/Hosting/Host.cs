using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;
using StructureMap;

namespace TCPIssue.Hosting
{
    public class Host
    {
        private static readonly ILog Log = LogManager.GetLogger<Host>();

        private readonly IContainer container;
        private readonly IReadOnlyCollection<IConfigureEndpoint> configs;

        private IEndpointInstance endpoint;

        public string EndpointName { get; }

        public static Host Create<TConfig>(string endpointName, params Action<NServiceBus.EndpointConfiguration>[] configureActions) 
            where TConfig : IConfigureEndpoint
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; // ensure that TLS 1.2 is enabled for all endpoints

            var container = IoC.CreateContainer();

            var configureEndpoints = new List<IConfigureEndpoint> { container.GetInstance<TConfig>() };
            configureEndpoints.AddRange(configureActions
                .Where(c => c != null)
                .Select(a => new ActionConfigureEndpoint(a)));

            var host = new Host(endpointName, container, configureEndpoints);

            return host;
        }

        public Host(string endpointName, IContainer container, IReadOnlyCollection<IConfigureEndpoint> configs)
        {
            if (string.IsNullOrWhiteSpace(endpointName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(endpointName));

            if (configs == null) throw new ArgumentNullException(nameof(configs));
            if (configs.Count == 0) throw new ArgumentException("Value cannot be empty.", nameof(configs));
            if (configs.Any(c => c == null)) throw new ArgumentException("Value cannot have null items.", nameof(configs));

            EndpointName = endpointName;

            this.container = container ?? throw new ArgumentNullException(nameof(container));
            this.configs = configs;
        }

        public async Task Start()
        {
            try
            {
                var endpointConfiguration = new NServiceBus.EndpointConfiguration(EndpointName);

                //These 2 settings happen for all endpoints
                endpointConfiguration.DefineCriticalErrorAction(OnCriticalError);
                endpointConfiguration.UseContainer<StructureMapBuilder>(x => x.ExistingContainer(container));

                foreach (var configure in configs)
                {
                    configure.Configure(endpointConfiguration);
                }

                endpoint = await Endpoint.Start(endpointConfiguration);
            }
            catch (Exception ex)
            {
                FailFast("Failed to start.", ex);
            }
        }

        public async Task Stop()
        {
            try
            {
                await (endpoint?.Stop() ?? Task.CompletedTask);
            }
            catch (Exception ex)
            {
                FailFast("Failed to stop correctly.", ex);
            }
        }

        private async Task OnCriticalError(ICriticalErrorContext context)
        {
            // We are intentionally exiting the process. We have Windows Service recovery to auto-restart.
            // https://docs.particular.net/nservicebus/hosting/critical-errors
            // https://docs.particular.net/nservicebus/hosting/windows-service#installation-restart-recovery
            try
            {
                await context.Stop();
            }
            finally
            {
                FailFast($"Critical error, shutting down: {context.Error}", context.Exception);
            }
        }

        private void FailFast(string message, Exception exception)
        {
            try
            {
                Log.Fatal(message, exception);
            }
            finally
            {
                //Cleaner exceptions in VS
                if (System.Diagnostics.Debugger.IsAttached)
                    throw exception;

                Environment.FailFast(message, exception);
            }
        }
    }
}
