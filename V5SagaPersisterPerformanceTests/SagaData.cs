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
}
