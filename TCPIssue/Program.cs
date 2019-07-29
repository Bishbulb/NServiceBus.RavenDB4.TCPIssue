using System.Threading.Tasks;
using TCPIssue.Hosting;

namespace TCPIssue
{
    class Program
    {
        public static async Task Main(string[] args) => await NServiceBusRunner.RunWithConfiguration<ConfigureEndpoint>(args, typeof(Program));
    }
}
