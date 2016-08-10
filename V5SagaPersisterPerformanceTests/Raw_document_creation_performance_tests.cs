using System;
using NUnit.Framework;
using Raven.Client.Document;
using Raven.Client.Document.DTC;
using NServiceBus.Saga;
using Raven.Client;
using System.Diagnostics;
using NServiceBus.RavenDB.Persistence;
using System.IO;
using System.Threading;
using System.Collections.Generic;

namespace V5SagaPersisterPerformanceTests
{
    [TestFixture]
    public class Raw_document_creation_performance_tests
    {
        [TestCase(1)]
        [TestCase(50000)]
        [TestCase(50000, 10)]
        [TestCase(50000, 32)]
        public void create_raw_doc(int howMany, int parallelization = 1)
        {
            var store = new DocumentStore()
            {
                Url = System.Configuration.ConfigurationManager.AppSettings[ "RavenDB/Url" ],
                DefaultDatabase = "V5RawDocPerfTests",
                TransactionRecoveryStorage = new LocalDirectoryTransactionRecoveryStorage(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dtc-storage"))
            };
            store.Initialize();

            var count = 0;
            var sw = Stopwatch.StartNew();

            var pending = new List<WaitHandle>();
            for(int i = 0; i < parallelization; i++)
            {
                var h = new ManualResetEvent(false);
                var t = new Thread(() =>
                {
                    try
                    {
                        while(count < howMany)
                        {
                            using(var session = store.OpenSession())
                            {
                                session.Store(new SagaData()
                                {
                                    Id = Guid.NewGuid()
                                });
                                session.SaveChanges();
                            }

                            Interlocked.Increment(ref count);
                        }
                    }
                    finally
                    {
                        h.Set();
                    }
                });
                pending.Add(h);

                t.Start();
            }

            var timeout = TimeSpan.FromSeconds(2);
            if(howMany > 50)
            {
                timeout = TimeSpan.FromSeconds(howMany / 50);
            }

            WaitHandle.WaitAll(pending.ToArray(), timeout);

            sw.Stop();

            TestContext.WriteLine($"Inserted: {count}");
            TestContext.WriteLine($"Elapsed (ms): {sw.ElapsedMilliseconds}");
            TestContext.WriteLine($"Elapsed: {sw.Elapsed}");
        }
    }
}
