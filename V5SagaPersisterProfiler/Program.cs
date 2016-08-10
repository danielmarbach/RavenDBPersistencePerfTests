using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using V5SagaPersisterPerformanceTests;

namespace V5SagaPersisterProfiler
{
    class Program
    {
        static void Main(string[] args)
        {
            var test = new Saga_creation_performance_tests();
            test.create_saga(2000, 1, false);
        }
    }
}
