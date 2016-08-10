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
using Raven.Abstractions.Data;
using System.Configuration;

namespace V6SagaPersisterPerformanceTests
{
    [TestFixture]
    public class Raw_document_with_unique_identity_creation_performance_tests
    {
        const string UniqueDocIdKey = "NServiceBus-UniqueDocId";

        [TestCase(1)]
        [TestCase(50000)]
        [TestCase(50000, 10)]
        [TestCase(50000, 32)]
        public async Task create_raw_doc_with_unique_identity(int howMany, int parallelization = 1)
        {
            var store = new DocumentStore()
            {
                Url = System.Configuration.ConfigurationManager.AppSettings[ "RavenDB/Url" ],
                DefaultDatabase = "V6RawDocPerfTests",
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

            var pending = new List<Task>();
            for(int i = 0; i < parallelization; i++)
            {
                var t = Task.Run(async () =>
                {
                    while(count < howMany)
                    {
                        using(var session = store.OpenAsyncSession())
                        {
                            var data = new SagaData()
                            {
                                Id = Guid.NewGuid()
                            };

                            await session.StoreAsync(data).ConfigureAwait(false);

                            var docId = session.Advanced.DocumentStore.Conventions.FindFullDocumentKeyFromNonStringIdentifier(data.Id, data.GetType(), false);
                            var uniqueDocId = UniqueDocIdKey + "/" + docId;

                            await session.StoreAsync(new 
                            {
                                Id = uniqueDocId,
                                SagaId = data.Id,
                                SagaDocId = docId
                            }, id: uniqueDocId, etag: Etag.Empty).ConfigureAwait(false);

                            var metadata = await session.Advanced.GetMetadataForAsync(data).ConfigureAwait(false);
                            metadata[ UniqueDocIdKey ] = uniqueDocId;

                            await session.SaveChangesAsync().ConfigureAwait(false);
                        }

                        Interlocked.Increment(ref count);
                    }
                });

                await t.ConfigureAwait(false);

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
