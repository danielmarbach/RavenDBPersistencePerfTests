using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using V6SagaPersisterPerformanceTests;

namespace V6SagaPersisterProfiler
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            var test = new Saga_creation_performance_tests();
            await test.create_saga(2000, 1, false);
        }
    }
}
