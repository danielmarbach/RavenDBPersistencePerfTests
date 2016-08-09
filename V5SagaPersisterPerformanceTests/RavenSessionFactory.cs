using NServiceBus.RavenDB.Persistence;
using Raven.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V5SagaPersisterPerformanceTests
{
    class RavenSessionFactory : ISessionProvider
    {
        IDocumentSession session;
        readonly IDocumentStore store;

        public RavenSessionFactory(IDocumentStore store)
        {
            session = null;
            this.store = store;
        }

        public IDocumentSession Session { get { return session ?? (session = OpenSession()); } }

        IDocumentSession OpenSession()
        {
            var documentSession = store.OpenSession();
            documentSession.Advanced.AllowNonAuthoritativeInformation = false;
            documentSession.Advanced.UseOptimisticConcurrency = true;
            return documentSession;
        }

        public void ReleaseSession()
        {
            if(session == null)
                return;

            session.Dispose();
            session = null;
        }

        public void SaveChanges()
        {
            if(session == null)
                return;

            session.SaveChanges();
        }
    }
}
