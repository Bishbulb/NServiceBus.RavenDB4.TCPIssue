using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using NServiceBus;
using Raven.Client.Documents;

namespace TCPIssue.Hosting
{
    public class ConfigureEndpoint : IConfigureEndpoint
    {
        public void Configure(EndpointConfiguration endpointConfiguration)
        {
            endpointConfiguration.EnableOutbox();

            endpointConfiguration.UseSerialization<NewtonsoftSerializer>();

            var rabbitMqConnectionString = ConfigurationManager.AppSettings["NServiceBus/RabbitMQ"];
            var transport = endpointConfiguration.UseTransport<RabbitMQTransport>()
                .ConnectionString(rabbitMqConnectionString)
                .UseConventionalRoutingTopology()
                .Transactions(TransportTransactionMode.ReceiveOnly);
        
            var documentStore = BuildDocumentStore("TCPIssue");
            endpointConfiguration.UsePersistence<RavenDBPersistence>()
                .DoNotSetupDatabasePermissions()
                .SetDefaultDocumentStore(documentStore);

            endpointConfiguration.SendFailedMessagesTo("error");
            endpointConfiguration.AuditProcessedMessagesTo("audit");
            endpointConfiguration.SendHeartbeatTo("Particular.ServiceControl.RabbitMQ");

            endpointConfiguration.EnableInstallers();
        }
        
        private static DocumentStore BuildDocumentStore(string database = null)
        {
            var urlsAppSetting = ConfigurationManager.AppSettings["Raven/Urls"];
            var urls = urlsAppSetting.Split(';').Select(x => x?.Trim()).ToArray();
            var certBase64 = ConfigurationManager.AppSettings["Raven/Certificate/Base64"];
            var certPassword = ConfigurationManager.AppSettings["Raven/Certificate/Password"];

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            
            var store = new DocumentStore
            {
                Urls = urls,
                Database = database,
                Conventions =
                {
                    IdentityPartsSeparator = "-",
                    MaxNumberOfRequestsPerSession = 250,
                    UseOptimisticConcurrency = true
                }
            };
            
            if (!string.IsNullOrWhiteSpace(certBase64))
            {
                var bytes = Convert.FromBase64String(certBase64);
                var x509Cert = new X509Certificate2(bytes, certPassword, X509KeyStorageFlags.MachineKeySet);
                store.Certificate = x509Cert;
            }

            store.Initialize();
            return store;
        }
    }
}