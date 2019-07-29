using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NSB.RDB4.Test.Hosting;

namespace NSB.RDB4.Test
{
    class Program
    {
        public static async Task Main(string[] args) => await NServiceBusRunner.RunWithConfiguration<ConfigureEndpoint>(args, typeof(Program));
    }
}
