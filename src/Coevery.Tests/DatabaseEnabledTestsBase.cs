﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Autofac;
using NHibernate;
using NUnit.Framework;
using Coevery.ContentManagement.FieldStorage.InfosetStorage;
using Coevery.ContentManagement.Handlers;
using Coevery.Data;
using Coevery.Environment.Configuration;
using Coevery.Services;
using Coevery.Tests.Data;
using Coevery.Tests.Stubs;

namespace Coevery.Tests {
    public abstract class DatabaseEnabledTestsBase {

        protected IContainer _container;

        protected ISession _session;
        protected string _databaseFilePath;
        protected ISessionFactory _sessionFactory;
        protected StubClock _clock;


        [TestFixtureSetUp]
        public void InitFixture() {
        }

        [TestFixtureTearDown]
        public void TearDownFixture() {
            File.Delete(_databaseFilePath);
        }

        [SetUp]
        public virtual void Init() {
            _databaseFilePath = Path.GetTempFileName();
            _sessionFactory = DataUtility.CreateSessionFactory(_databaseFilePath, DatabaseTypes.ToArray());
            _session = _sessionFactory.OpenSession();
            _clock = new StubClock();

            var builder = new ContainerBuilder();
            //builder.RegisterModule(new ImplicitCollectionSupportModule());
            builder.RegisterType<InfosetHandler>().As<IContentHandler>();
            builder.RegisterInstance(new StubLocator(_session)).As<ISessionLocator>();
            builder.RegisterInstance(_clock).As<IClock>();
            builder.RegisterGeneric(typeof(Repository<>)).As(typeof(IRepository<>));
            builder.RegisterInstance(new ShellSettings { Name = ShellSettings.DefaultName, DataProvider = "SqlCe" });
            
            Register(builder);
            _container = builder.Build();
        }

        [TearDown]
        public void Cleanup() {
            if(_container != null)
                _container.Dispose();

            if(_session != null)
                _session.Close();
        }

        public abstract void Register(ContainerBuilder builder);

        protected virtual IEnumerable<Type> DatabaseTypes {
            get {
                return Enumerable.Empty<Type>();
            }
        }

        protected void ClearSession() {
            Trace.WriteLine("Flush and clear session");
            _session.Flush();
            _session.Clear();
            Trace.WriteLine("Flushed and cleared session");
        }
    }
}