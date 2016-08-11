using NServiceBus;
using System;

namespace V6SagaPersisterPerformanceTests
{
    class SagaData : ContainSagaData
    {
        
    }

    class SagaUniqueDocument
    {
        public string Id { get; internal set; }
        public string SagaDocId { get; internal set; }
        public Guid SagaId { get; internal set; }
    }
}
