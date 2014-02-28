﻿using System;
using System.Web.Security;
using System.Xml.Linq;
using Autofac;
using Moq;
using NHibernate;
using NUnit.Framework;
using Coevery.Caching;
using Coevery.ContentManagement.FieldStorage.InfosetStorage;
using Coevery.ContentManagement.MetaData;
using Coevery.ContentManagement.MetaData.Models;
using Coevery.ContentManagement.MetaData.Services;
using Coevery.Core.Settings.Metadata;
using Coevery.Data;
using Coevery.ContentManagement;
using Coevery.ContentManagement.Handlers;
using Coevery.ContentManagement.Records;
using Coevery.DisplayManagement;
using Coevery.DisplayManagement.Descriptors;
using Coevery.DisplayManagement.Implementation;
using Coevery.Environment;
using Coevery.Environment.Configuration;
using Coevery.Environment.Extensions;
using Coevery.Messaging.Events;
using Coevery.Messaging.Services;
using Coevery.Security;
using Coevery.Tests.Stubs;
using Coevery.Tests.Utility;
using Coevery.UI.PageClass;
using Coevery.Users.Handlers;
using Coevery.Users.Models;
using Coevery.Users.Services;

namespace Coevery.Tests.Modules.Users.Services {
    [TestFixture]
    public class MembershipServiceTests {
        private IMembershipService _membershipService;
        private ISessionFactory _sessionFactory;
        private ISession _session;
        private IContainer _container;


        public class TestSessionLocator : ISessionLocator {
            private readonly ISession _session;

            public TestSessionLocator(ISession session) {
                _session = session;
            }

            public ISession For(Type entityType) {
                return _session;
            }
        }

        [TestFixtureSetUp]
        public void InitFixture() {
            var databaseFileName = System.IO.Path.GetTempFileName();
            _sessionFactory = DataUtility.CreateSessionFactory(
                databaseFileName,
                typeof(UserPartRecord),
                typeof(ContentItemVersionRecord),
                typeof(ContentItemRecord),
                typeof(ContentTypeRecord));
        }

        [TestFixtureTearDown]
        public void TermFixture() {

        }

        [SetUp]
        public void Init() {
            var builder = new ContainerBuilder();
            //builder.RegisterModule(new ImplicitCollectionSupportModule());
            builder.RegisterType<MembershipService>().As<IMembershipService>();
            builder.RegisterType<DefaultContentQuery>().As<IContentQuery>();
            builder.RegisterType<DefaultContentManager>().As<IContentManager>();
            builder.RegisterType<StubCacheManager>().As<ICacheManager>();
            builder.RegisterType<Signals>().As<ISignals>();
            builder.RegisterType(typeof(SettingsFormatter)).As<ISettingsFormatter>();
            builder.RegisterType<ContentDefinitionManager>().As<IContentDefinitionManager>();
            builder.RegisterType<DefaultContentManagerSession>().As<IContentManagerSession>();
            builder.RegisterInstance(new ShellSettings { Name = ShellSettings.DefaultName, DataProvider = "SqlCe" });
            builder.RegisterType<UserPartHandler>().As<IContentHandler>();
            builder.RegisterType<StubWorkContextAccessor>().As<IWorkContextAccessor>();
            builder.RegisterType<CoeveryServices>().As<ICoeveryServices>();
            builder.RegisterAutoMocking(MockBehavior.Loose);
            builder.RegisterGeneric(typeof(Repository<>)).As(typeof(IRepository<>));
            builder.RegisterInstance(new Mock<IMessageEventHandler>().Object);
            builder.RegisterType<DefaultMessageManager>().As<IMessageManager>();
            builder.RegisterType<DefaultShapeTableManager>().As<IShapeTableManager>();
            builder.RegisterType<DefaultShapeFactory>().As<IShapeFactory>();
            builder.RegisterType<StubExtensionManager>().As<IExtensionManager>();
            builder.RegisterInstance(new Mock<IPageClassBuilder>().Object);
            builder.RegisterType<DefaultContentDisplay>().As<IContentDisplay>();
            builder.RegisterType<InfosetHandler>().As<IContentHandler>();

            _session = _sessionFactory.OpenSession();
            builder.RegisterInstance(new TestSessionLocator(_session)).As<ISessionLocator>();
            _container = builder.Build();
            _membershipService = _container.Resolve<IMembershipService>();
        }

        [Test]
        public void CreateUserShouldAllocateModelAndCreateRecords() {
            var user = _membershipService.CreateUser(new CreateUserParams("a", "b", "c", null, null, true));
            Assert.That(user.UserName, Is.EqualTo("a"));
            Assert.That(user.Email, Is.EqualTo("c"));
        }

        [Test]
        public void DefaultPasswordFormatShouldBeHashedAndHaveSalt() {
            var user = _membershipService.CreateUser(new CreateUserParams("a", "b", "c", null, null, true));

            var userRepository = _container.Resolve<IRepository<UserPartRecord>>();
            var userRecord = userRepository.Get(user.Id);
            Assert.That(userRecord.PasswordFormat, Is.EqualTo(MembershipPasswordFormat.Hashed));
            Assert.That(userRecord.Password, Is.Not.EqualTo("b"));
            Assert.That(userRecord.PasswordSalt, Is.Not.Null);
            Assert.That(userRecord.PasswordSalt, Is.Not.Empty);
        }

        [Test]
        public void SaltAndPasswordShouldBeDifferentEvenWithSameSourcePassword() {
            var user1 = _membershipService.CreateUser(new CreateUserParams("a", "b", "c", null, null, true));
            _session.Flush();
            _session.Clear();

            var user2 = _membershipService.CreateUser(new CreateUserParams("d", "b", "e", null, null, true));
            _session.Flush();
            _session.Clear();

            var userRepository = _container.Resolve<IRepository<UserPartRecord>>();
            var user1Record = userRepository.Get(user1.Id);
            var user2Record = userRepository.Get(user2.Id);
            Assert.That(user1Record.PasswordSalt, Is.Not.EqualTo(user2Record.PasswordSalt));
            Assert.That(user1Record.Password, Is.Not.EqualTo(user2Record.Password));

            Assert.That(_membershipService.ValidateUser("a", "b"), Is.Not.Null);
            Assert.That(_membershipService.ValidateUser("d", "b"), Is.Not.Null);
        }

        [Test]
        public void ValidateUserShouldReturnNullIfUserOrPasswordIsIncorrect() {
            _membershipService.CreateUser(new CreateUserParams("test-user", "test-password", "c", null, null, true));
            _session.Flush();
            _session.Clear();

            var validate1 = _membershipService.ValidateUser("test-user", "bad-password");
            var validate2 = _membershipService.ValidateUser("bad-user", "test-password");
            var validate3 = _membershipService.ValidateUser("test-user", "test-password");

            Assert.That(validate1, Is.Null);
            Assert.That(validate2, Is.Null);
            Assert.That(validate3, Is.Not.Null);
        }
    }
}
