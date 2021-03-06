﻿using System;
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
using System.Configuration;

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
                    try
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
                                //var uniqueDocId = UniqueDocIdKey + "/" + docId;
                                var uniqueDocId = UniqueDocument.FormatId(data.GetType(), "Id", data.Id);

                                session.Store(new SagaUniqueDocument()
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

            TestContext.WriteLine($"Inserted: {count}");
            TestContext.WriteLine($"Elapsed (ms): {sw.ElapsedMilliseconds}");
            TestContext.WriteLine($"Elapsed: {sw.Elapsed}");
        }
    }
}
