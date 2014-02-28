using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Coevery.Environment;
using Coevery.Mvc.ViewEngines.ThemeAwareness;
using Moq;
using NUnit.Framework;
using Coevery.Caching;
using Coevery.DisplayManagement.Descriptors;
using Coevery.DisplayManagement.Descriptors.ShapeTemplateStrategy;
using Coevery.Environment.Descriptor.Models;
using Coevery.Environment.Extensions;
using Coevery.Environment.Extensions.Models;
using Coevery.FileSystems.VirtualPath;
using Coevery.Tests.Stubs;

namespace Coevery.Tests.DisplayManagement.Descriptors {
    [TestFixture]
    public class ShapeTemplateBindingStrategyTests : ContainerTestBase {
        private ShellDescriptor _descriptor;
        private IList<FeatureDescriptor> _features;
        private TestViewEngine _testViewEngine;
        private TestVirtualPathProvider _testVirtualPathProvider;


        protected override void Register(ContainerBuilder builder) {
            _descriptor = new ShellDescriptor();
            _testViewEngine = new TestViewEngine();
            _testVirtualPathProvider = new TestVirtualPathProvider();

            builder.Register(ctx => _descriptor);
            builder.RegisterType<StubCacheManager>().As<ICacheManager>();
            builder.RegisterType<StubParallelCacheContext>().As<IParallelCacheContext>();
            builder.RegisterType<StubVirtualPathMonitor>().As<IVirtualPathMonitor>();
            builder.RegisterType<TestLayoutAwareViewEngine>().As<ILayoutAwareViewEngine>();
            builder.RegisterType<ShapeTemplateBindingStrategy>().As<IShapeTableProvider>();
            builder.RegisterType<StubWorkContextAccessor>().As<IWorkContextAccessor>();
            builder.RegisterType<BasicShapeTemplateHarvester>().As<IShapeTemplateHarvester>();
            builder.RegisterInstance(_testViewEngine).As<IShapeTemplateViewEngine>();
            builder.RegisterInstance(_testVirtualPathProvider).As<IVirtualPathProvider>();

            var extensionManager = new Mock<IExtensionManager>();
            builder.Register(ctx => extensionManager);
            builder.Register(ctx => extensionManager.Object);
            builder.RegisterSource(new WorkRegistrationSource());
        }

        class WorkValues<T> where T : class
        {
            public WorkValues(IComponentContext componentContext)
            {
                ComponentContext = componentContext;
                Values = new Dictionary<Work<T>, T>();
            }

            public IComponentContext ComponentContext { get; private set; }
            public IDictionary<Work<T>, T> Values { get; private set; }
        }

        class WorkRegistrationSource : IRegistrationSource
        {
            static readonly MethodInfo CreateMetaRegistrationMethod = typeof(WorkRegistrationSource).GetMethod(
                "CreateMetaRegistration", BindingFlags.Static | BindingFlags.NonPublic);

            private static bool IsClosingTypeOf(Type type, Type openGenericType)
            {
                return type.IsGenericType && type.GetGenericTypeDefinition() == openGenericType;
            }

            public IEnumerable<IComponentRegistration> RegistrationsFor(Service service, Func<Service, IEnumerable<IComponentRegistration>> registrationAccessor)
            {
                var swt = service as IServiceWithType;
                if (swt == null || !IsClosingTypeOf(swt.ServiceType, typeof(Work<>)))
                    return Enumerable.Empty<IComponentRegistration>();

                var valueType = swt.ServiceType.GetGenericArguments()[0];

                var valueService = swt.ChangeType(valueType);

                var registrationCreator = CreateMetaRegistrationMethod.MakeGenericMethod(valueType);

                return registrationAccessor(valueService)
                    .Select(v => registrationCreator.Invoke(null, new object[] { service, v }))
                    .Cast<IComponentRegistration>();
            }

            public bool IsAdapterForIndividualComponents
            {
                get { return true; }
            }

            static IComponentRegistration CreateMetaRegistration<T>(Service providedService, IComponentRegistration valueRegistration) where T : class
            {
                var rb = RegistrationBuilder.ForDelegate(
                    (c, p) =>
                    {
                        var workContextAccessor = c.Resolve<IWorkContextAccessor>();
                        return new Work<T>(w =>
                        {
                            var workContext = workContextAccessor.GetContext();
                            if (workContext == null)
                                return default(T);

                            var workValues = workContext.Resolve<WorkValues<T>>();

                            T value;
                            if (!workValues.Values.TryGetValue(w, out value))
                            {
                                value = (T)workValues.ComponentContext.ResolveComponent(valueRegistration, p);
                                workValues.Values[w] = value;
                            }
                            return value;
                        });
                    })
                    .As(providedService)
                    .Targeting(valueRegistration);

                return rb.CreateRegistration();
            }
        }

        public class TestViewEngine : Dictionary<string, object>, IShapeTemplateViewEngine {
            public IEnumerable<string> DetectTemplateFileNames(IEnumerable<string> fileNames) {
                return fileNames;
            }
        }

        public class TestLayoutAwareViewEngine : ILayoutAwareViewEngine {

            public System.Web.Mvc.ViewEngineResult FindPartialView(System.Web.Mvc.ControllerContext controllerContext, string partialViewName, bool useCache)
            {
                throw new NotImplementedException();
            }

            public System.Web.Mvc.ViewEngineResult FindView(System.Web.Mvc.ControllerContext controllerContext, string viewName, string masterName, bool useCache)
            {
                throw new NotImplementedException();
            }

            public void ReleaseView(System.Web.Mvc.ControllerContext controllerContext, System.Web.Mvc.IView view)
            {
                throw new NotImplementedException();
            }
        }

        public class TestVirtualPathProvider : IVirtualPathProvider {
            public string Combine(params string[] paths) {
                throw new NotImplementedException();
            }

            public string ToAppRelative(string virtualPath) {
                throw new NotImplementedException();
            }

            public string MapPath(string virtualPath) {
                throw new NotImplementedException();
            }

            public bool FileExists(string virtualPath) {
                throw new NotImplementedException();
            }

            public Stream OpenFile(string virtualPath) {
                throw new NotImplementedException();
            }

            public StreamWriter CreateText(string virtualPath) {
                throw new NotImplementedException();
            }

            public Stream CreateFile(string virtualPath) {
                throw new NotImplementedException();
            }

            public DateTime GetFileLastWriteTimeUtc(string virtualPath) {
                throw new NotImplementedException();
            }

            public string GetFileHash(string virtualPath) {
                throw new NotImplementedException();
            }

            public string GetFileHash(string virtualPath, IEnumerable<string> dependencies) {
                throw new NotImplementedException();
            }

            public void DeleteFile(string virtualPath) {
                throw new NotImplementedException();
            }

            public bool DirectoryExists(string virtualPath) {
                return true;
            }

            public void CreateDirectory(string virtualPath) {
                throw new NotImplementedException();
            }

            public virtual void DeleteDirectory(string virtualPath) {
                throw new NotImplementedException();
            }

            public string GetDirectoryName(string virtualPath) {
                throw new NotImplementedException();
            }

            public IEnumerable<string> ListFiles(string path) {
                return new List<string> {"~/Modules/Alpha/Views/AlphaShape.blah"};
            }

            public IEnumerable<string> ListDirectories(string path) {
                throw new NotImplementedException();
            }

            public bool TryFileExists(string virtualPath) {
                throw new NotImplementedException();
            }
        }

        protected override void Resolve(ILifetimeScope container) {
            _features = new List<FeatureDescriptor>();

            container.Resolve<Mock<IExtensionManager>>()
                .Setup(em => em.AvailableFeatures())
                .Returns(_features);
        }

        void AddFeature(string name, params string[] dependencies) {
            var featureDescriptor = new FeatureDescriptor {
                Id = name,
                Dependencies = dependencies,
                Extension = new ExtensionDescriptor {
                    Id = name,
                    Location = "~/Modules"
                }
            };
            featureDescriptor.Extension.Features = new[] { featureDescriptor };

            _features.Add(featureDescriptor);
        }

        void AddEnabledFeature(string name, params string[] dependencies) {
            AddFeature(name, dependencies);
            _descriptor.Features = _descriptor.Features.Concat(new[] { new ShellFeature { Name = name } });
        }

        [Test]
        public void TemplateResolutionWorks() {
            AddEnabledFeature("Alpha");

            _testViewEngine.Add("~/Modules/Alpha/Views/AlphaShape.blah", null);
            var strategy = _container.Resolve<IShapeTableProvider>();

            var builder = new ShapeTableBuilder(null);
            strategy.Discover(builder);
            var alterations = builder.BuildAlterations();

            Assert.That(alterations.Any(alteration => alteration.ShapeType.Equals("AlphaShape", StringComparison.OrdinalIgnoreCase)));
        }
    }
}
