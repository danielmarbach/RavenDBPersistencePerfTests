using NServiceBus.Saga;
using System;

namespace V5SagaPersisterPerformanceTests
{
    class SagaData : ContainSagaData
    {
        [Unique]
        public override Guid Id
        {
            get;
            set;
        }
    }

    class SagaUniqueDocument
    {
        public string Id { get; internal set; }
        public string SagaDocId { get; internal set; }
        public Guid SagaId { get; internal set; }
    }
}
