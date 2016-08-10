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
using Raven.Abstractions.Data;

namespace V5SagaPersisterPerformanceTests
{
    [TestFixture]
    public class Raw_document_with_unique_identity_creation_performance_tests
    {
        const string UniqueDocIdKey = "NServiceBus-UniqueDocId";

        [TestCase(1)]
        [TestCase(50000)]
        [TestCase(50000, 10)]
        [TestCase(50000, 32)]
        public void create_raw_doc_with_unique_identity(int howMany, int parallelization = 1)
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
                    while(count < howMany)
                    {
                        using(var session = store.OpenSession())
                        {
                            var data = new SagaData()
                            {
                                Id = Guid.NewGuid()
                            };

                            session.Store(data);

                            var docId = session.Advanced.DocumentStore.Conventions.FindFullDocumentKeyFromNonStringIdentifier(data.Id, data.GetType(), false);
                            var uniqueDocId = UniqueDocIdKey + "/" + docId;

                            session.Store(new
                            {
                                Id = uniqueDocId,
                                SagaId = data.Id,
                                SagaDocId = docId
                            }, id: uniqueDocId, etag: Etag.Empty);

                            var metadata = session.Advanced.GetMetadataFor(data);
                            metadata[ UniqueDocIdKey ] = uniqueDocId;

                            session.SaveChanges();
                        }

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
