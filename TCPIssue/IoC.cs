using Raven.Client.Documents.Session;
using StructureMap;
using StructureMap.Graph.Scanning;

namespace TCPIssue
{
    public static class IoC
    {
        public static IContainer CreateContainer()
        {
            IContainer container = new Container();
            container.Configure(x =>
            {
                x.Policies.FillAllPropertiesOfType<IAsyncDocumentSession>();
                x.Scan(s =>
                {
                    s.AssembliesAndExecutablesFromApplicationBaseDirectory(t => t.FullName.StartsWith("TCPIssue"));
                    s.WithDefaultConventions();
                    s.LookForRegistries();
                });
            });
            
            TypeRepository.AssertNoTypeScanningFailures();

            return container;
        }
    }
}