using NServiceBus.Sagas;
using NServiceBus.Testing;
using NUnit.Framework;
using Raven.Client.Document;
using Raven.Client.Document.DTC;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NServiceBus.Persistence;
using System.IO;
using System.Threading;

namespace V6SagaPersisterPerformanceTests
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
        public async Task create_saga(int howMany, int parallelization = 1)
        {
            var store = new DocumentStore()
            {
                Url = System.Configuration.ConfigurationManager.AppSettings[ "RavenDB/Url" ],
                DefaultDatabase = "V6SagaPersisterPerfTests",
                TransactionRecoveryStorage = new LocalDirectoryTransactionRecoveryStorage(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dtc-storage"))
            };
            store.Initialize();

            var sagaPersisterType = Type.GetType("NServiceBus.Persistence.RavenDB.SagaPersister, NServiceBus.RavenDB");
            var sagaPersister = (ISagaPersister)Activator.CreateInstance(sagaPersisterType);

            var count = 0;
            var sw = Stopwatch.StartNew();

            var pending = new List<Task>();
            for(int i = 0; i < parallelization; i++)
            {
                var t = Task.Run(async () =>
                {
                    while(count < howMany)
                    {
                        var data = new SagaData()
                        {
                            Id = Guid.NewGuid()
                        };

                        using(var session = store.OpenAsyncSession())
                        {
                            var storageSession = (SynchronizedStorageSession)Type.GetType("NServiceBus.Persistence.RavenDB.RavenDBSynchronizedStorageSession, NServiceBus.RavenDB").GetInstance(session, false);

                            await sagaPersister.Save(data, new SagaCorrelationProperty("Id", data.Id), storageSession, new NServiceBus.Extensibility.ContextBag()).ConfigureAwait(false);
                            await session.SaveChangesAsync().ConfigureAwait(false);
                        }

                        Interlocked.Increment(ref count);
                    }
                });
                pending.Add(t);
            }

            await Task.WhenAll(pending);

            sw.Stop();

            TestContext.WriteLine($"Inserted: {count}");
            TestContext.WriteLine($"Elapsed (ms): {sw.ElapsedMilliseconds}");
            TestContext.WriteLine($"Elapsed: {sw.Elapsed}");
        }
    }
}
