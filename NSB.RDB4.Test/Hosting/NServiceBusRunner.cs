using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace NSB.RDB4.Test.Hosting
{
    public static class NServiceBusRunner
    {
        public static Task RunWithConfiguration<TConfig>(string[] args, Type typeInEndpointAssembly, params Action<NServiceBus.EndpointConfiguration>[] customizeConfig) 
            where TConfig : IConfigureEndpoint =>
            RunWithConfiguration<TConfig>(args, EndpointNameFromType(typeInEndpointAssembly), customizeConfig);

        public static async Task RunWithConfiguration<TConfig>(string[] args, string endpointName, params Action<NServiceBus.EndpointConfiguration>[] configureActions)
            where TConfig : IConfigureEndpoint
        {
            var host = Host.Create<TConfig>(endpointName, configureActions);

            // pass this command line option to run as a windows service
            if (args.Contains("--run-as-service"))
            {
                using (var windowsService = new WindowsService(host))
                {
                    ServiceBase.Run(windowsService);
                    return;
                }
            }

            Console.Title = FormatTitle(endpointName);

            var tcs = new TaskCompletionSource<object>();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                tcs.SetResult(null);
            };

            await host.Start();
            await Console.Out.WriteLineAsync("Press Ctrl+C to exit...");

            await tcs.Task;
            await host.Stop();
        }

        private static string EndpointNameFromType(Type t) => t.Assembly.GetName().Name;

        private static string FormatTitle(string endpointName)
        {
            const string gateway = "Gateway.";
            const string handlers = ".Handlers";

            if (endpointName.StartsWith(gateway))
                endpointName = endpointName.Remove(0, gateway.Length);
            if (endpointName.EndsWith(handlers))
                endpointName = endpointName.Remove(endpointName.Length - handlers.Length, handlers.Length);

            return endpointName;
        }
    }
}