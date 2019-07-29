using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Queries;

namespace RavenDB.Tryout
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var documentStore = BuildDocumentStore();

            var count = 10;
            for (var i=0; i<count; i++)
            {
                Console.WriteLine($"Issuing query #{i+1} of {count}...");

                var options = new QueryOperationOptions { AllowStale = true };
                var deleteOp = new DeleteByQueryOperation(new IndexQuery{Query = "from 'Foobars'"}, options);

                var operation = await documentStore.Operations.SendAsync(deleteOp).ConfigureAwait(false);
                await operation.WaitForCompletionAsync().ConfigureAwait(false);

                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        private static IDocumentStore BuildDocumentStore()
        {
            var urlsAppSetting = ConfigurationManager.AppSettings["Raven/Urls"];
            var urls = urlsAppSetting.Split(';').Select(x => x?.Trim()).ToArray();

            var store = new DocumentStore
            {
                Urls = urls,
                Database = "RavenDB.Tryouts",
                Conventions =
                {
                    IdentityPartsSeparator = "-",
                    MaxNumberOfRequestsPerSession = 250,
                    UseOptimisticConcurrency = true
                }
            };

            store.Initialize();
            return store;
        }
    }
}
