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
    public class Saga_creation_performance_tests
    {
        [TestCase(1)]
        [TestCase(1000)]
        [TestCase(1000, 10)]
        [TestCase(50000)]
        [TestCase(50000, 10)]
        [TestCase(50000, 32)]
        [TestCase(200000)]
        [TestCase(200000, 10)]
        public void create_saga(int howManySagas, int parallelization = 1)
        {
            var store = new DocumentStore()
            {
                Url = System.Configuration.ConfigurationManager.AppSettings[ "RavenDB/Url" ],
                DefaultDatabase = "V5SagaPersisterPerfTests",
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
                    var sessionProvider = new RavenSessionFactory(store);

                    var sagaPersisterType = Type.GetType("NServiceBus.SagaPersisters.RavenDB.SagaPersister, NServiceBus.RavenDB");
                    var sagaPersister = (ISagaPersister)Activator.CreateInstance(sagaPersisterType, new object[] { sessionProvider });


                    while(count < howManySagas)
                    {
                        var data = new SagaData()
                        {
                            Id = Guid.NewGuid()
                        };

                        sagaPersister.Save(data);
                        sessionProvider.SaveChanges();
                        sessionProvider.ReleaseSession();

                        Interlocked.Increment(ref count);
                    }

                    h.Set();
                });
                pending.Add(h);

                t.Start();
            }

            WaitHandle.WaitAll(pending.ToArray());

            sw.Stop();

            TestContext.WriteLine($"Inserted: {count}");
            TestContext.WriteLine($"Elapsed (ms): {sw.ElapsedMilliseconds}");
            TestContext.WriteLine($"Elapsed: {sw.Elapsed}");
        }
    }
}
