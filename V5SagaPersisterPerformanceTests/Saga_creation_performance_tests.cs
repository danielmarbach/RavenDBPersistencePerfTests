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
using System.Configuration;

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
        public void create_saga(int howMany, int parallelization = 1, bool traceOutput = true)
        {
            var store = new DocumentStore()
            {
                Url = System.Configuration.ConfigurationManager.AppSettings[ "RavenDB/Url" ],
                DefaultDatabase = "V5SagaPersisterPerfTests",
                TransactionRecoveryStorage = new LocalDirectoryTransactionRecoveryStorage(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dtc-storage"))
            };

            bool disableReplicationInformer;
            if(bool.TryParse(ConfigurationManager.AppSettings[ "RavenDB/ReplicationInformer/Disable" ], out disableReplicationInformer) && disableReplicationInformer)
            {
                store.Conventions.FailoverBehavior = Raven.Abstractions.Replication.FailoverBehavior.FailImmediately;
            }

            store.Initialize();

            var count = 0;
            var sw = Stopwatch.StartNew();

            var pending = new List<WaitHandle>();
            for(int i = 0; i < parallelization; i++)
            {
                var t = new Thread(obj =>
                {
                    var sessionProvider = new RavenSessionFactory(store);

                    var sagaPersister = (ISagaPersister)Activator.CreateInstance(Type.GetType("NServiceBus.SagaPersisters.RavenDB.SagaPersister, NServiceBus.RavenDB"), new object[] { sessionProvider });
                    //var sagaPersister = (ISagaPersister)Type.GetType("NServiceBus.SagaPersisters.RavenDB.SagaPersister, NServiceBus.RavenDB").GetInstance(sessionProvider);

                    try
                    {
                        while(count < howMany)
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

                    }
                    finally
                    {
                        ((ManualResetEvent)obj).Set();
                    }
                });

                var wh = new ManualResetEvent(false);
                pending.Add(wh);

                t.Start(wh);
            }

            var timeout = TimeSpan.FromSeconds(2);
            if(howMany > 50)
            {
                timeout = TimeSpan.FromSeconds(howMany / 50);
            }

            WaitHandle.WaitAll(pending.ToArray(), timeout);

            sw.Stop();

            if(traceOutput)
            {
                TestContext.WriteLine($"Inserted: {count}");
                TestContext.WriteLine($"Elapsed (ms): {sw.ElapsedMilliseconds}");
                TestContext.WriteLine($"Elapsed: {sw.Elapsed}");
            }
        }
    }
}
