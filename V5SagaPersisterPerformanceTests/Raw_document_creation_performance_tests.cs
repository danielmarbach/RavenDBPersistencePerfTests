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
                Url = "http://localhost:8083",
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
                            session.Store(new SagaData()
                            {
                                Id = Guid.NewGuid()
                            });
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