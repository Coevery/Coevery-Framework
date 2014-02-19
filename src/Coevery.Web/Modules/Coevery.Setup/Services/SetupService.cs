using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using Coevery.ContentManagement;
using Coevery.Core.Settings.Descriptor.Records;
using Coevery.Core.Settings.Models;
using Coevery.Core.Settings.Services;
using Coevery.Data;
using Coevery.Data.Migration;
using Coevery.Data.Migration.Interpreters;
using Coevery.Data.Migration.Schema;
using Coevery.Environment;
using Coevery.Environment.Configuration;
using Coevery.Environment.Descriptor;
using Coevery.Environment.Descriptor.Models;
using Coevery.Environment.ShellBuilders;
using Coevery.Environment.State;
using Coevery.Localization;
using Coevery.Localization.Services;
using Coevery.Recipes.Models;
using Coevery.Recipes.Services;
using Coevery.Reports.Services;
using Coevery.Security;
using Coevery.Settings;
using Coevery.Utility.Extensions;

namespace Coevery.Setup.Services {
    public class SetupService : ISetupService {
        private readonly ShellSettings _shellSettings;
        private readonly ICoeveryHost _CoeveryHost;
        private readonly IShellSettingsManager _shellSettingsManager;
        private readonly IShellContainerFactory _shellContainerFactory;
        private readonly ICompositionStrategy _compositionStrategy;
        private readonly IProcessingEngine _processingEngine;
        private readonly IRecipeHarvester _recipeHarvester;
        private readonly IEnumerable<Recipe> _recipes;

        public SetupService(
            ShellSettings shellSettings,
            ICoeveryHost CoeveryHost,
            IShellSettingsManager shellSettingsManager,
            IShellContainerFactory shellContainerFactory,
            ICompositionStrategy compositionStrategy,
            IProcessingEngine processingEngine,
            IRecipeHarvester recipeHarvester) {
            _shellSettings = shellSettings;
            _CoeveryHost = CoeveryHost;
            _shellSettingsManager = shellSettingsManager;
            _shellContainerFactory = shellContainerFactory;
            _compositionStrategy = compositionStrategy;
            _processingEngine = processingEngine;
            _recipeHarvester = recipeHarvester;
            _recipes = _recipeHarvester.HarvestRecipes("Coevery.Setup");
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        public ShellSettings Prime() {
            return _shellSettings;
        }

        public IEnumerable<Recipe> Recipes() {
            return _recipes;
        }

        public string Setup(SetupContext context) {
            string executionId;

            // The vanilla Coevery distibution has the following features enabled.
            string[] hardcoded = {
                    // Framework
                    "Coevery.Framework",
                    // Core
                    "Settings",
                    // Modules
                    "Coevery.Recipes", "Coevery.Users"
                };

            context.EnabledFeatures = hardcoded.Union(context.EnabledFeatures ?? Enumerable.Empty<string>()).Distinct().ToList();

            var shellSettings = new ShellSettings(_shellSettings);

            if (string.IsNullOrEmpty(shellSettings.DataProvider)) {
                shellSettings.DataProvider = context.DatabaseProvider;
                shellSettings.DataConnectionString = context.DatabaseConnectionString;
                shellSettings.DataTablePrefix = context.DatabaseTablePrefix;
            }

            #region Encryption Settings

            shellSettings.EncryptionAlgorithm = "AES";
            // randomly generated key
            shellSettings.EncryptionKey = SymmetricAlgorithm.Create(shellSettings.EncryptionAlgorithm).Key.ToHexString();

            shellSettings.HashAlgorithm = "HMACSHA256";
            // randomly generated key
            shellSettings.HashKey = HMAC.Create(shellSettings.HashAlgorithm).Key.ToHexString();
            
            #endregion

            var shellDescriptor = new ShellDescriptor {
                Features = context.EnabledFeatures.Select(name => new ShellFeature { Name = name })
            };

            var shellBlueprint = _compositionStrategy.Compose(shellSettings, shellDescriptor);

            var f = shellBlueprint.Dependencies.Where(d => d.Type == typeof(RecipeManager)).ToList();

            // initialize database explicitly, and store shell descriptor
            using (var bootstrapLifetimeScope = _shellContainerFactory.CreateContainer(shellSettings, shellBlueprint)) {

                using (var environment = bootstrapLifetimeScope.CreateWorkContextScope()) {

                    // check if the database is already created (in case an exception occured in the second phase)
                    var schemaBuilder = new SchemaBuilder(environment.Resolve<IDataMigrationInterpreter>());
                    try {
                        var tablePrefix = String.IsNullOrEmpty(shellSettings.DataTablePrefix) ? "" : shellSettings.DataTablePrefix + "_";
                        schemaBuilder.ExecuteSql("SELECT * FROM " + tablePrefix + "ShellDescriptorRecord");
                    }
                    catch {
                        var reportsCoordinator = environment.Resolve<IReportsCoordinator>();

                        reportsCoordinator.Register("Data Migration", "Setup", "Coevery installation");

                        schemaBuilder.CreateTable("DataMigrationRecord",
                                                  table => table
                                                               .Column<int>("Id", column => column.PrimaryKey().Identity())
                                                               .Column<string>("DataMigrationClass")
                                                               .Column<int>("Version"));

                        var dataMigrationManager = environment.Resolve<IDataMigrationManager>();
                        dataMigrationManager.Update("Settings");

                        foreach (var feature in context.EnabledFeatures) {
                            dataMigrationManager.Update(feature);
                        }

                        environment.Resolve<IShellDescriptorManager>().UpdateShellDescriptor(
                            0,
                            shellDescriptor.Features,
                            shellDescriptor.Parameters);
                    }
                }
            }



            // in effect "pump messages" see PostMessage circa 1980
            while ( _processingEngine.AreTasksPending() )
                _processingEngine.ExecuteNextTask();

            // creating a standalone environment. 
            // in theory this environment can be used to resolve any normal components by interface, and those
            // components will exist entirely in isolation - no crossover between the safemode container currently in effect

            // must mark state as Running - otherwise standalone enviro is created "for setup"
            shellSettings.State = TenantState.Running;
            using (var environment = _CoeveryHost.CreateStandaloneEnvironment(shellSettings)) {
                try {
                    executionId = CreateTenantData(context, environment);
                }
                catch {
                    environment.Resolve<ITransactionManager>().Cancel();
                    throw;
                }
            }

            _shellSettingsManager.SaveSettings(shellSettings);
 
            return executionId;
        }

        private string CreateTenantData(SetupContext context, IWorkContextScope environment) {

            // set site name and settings
            var siteService = environment.Resolve<ISiteService>();
            var siteSettings = siteService.GetSiteSettings().As<SiteSettingsPart>();
            siteSettings.SiteSalt = Guid.NewGuid().ToString("N");
            siteSettings.SiteName = context.SiteName;
            siteSettings.SuperUser = context.AdminUsername;
            siteSettings.SiteCulture = "en-US";

            // add default culture
            var cultureManager = environment.Resolve<ICultureManager>();
            cultureManager.AddCulture("en-US");

            var recipeManager = environment.Resolve<IRecipeManager>();
            string executionId = recipeManager.Execute(Recipes().FirstOrDefault(r => r.Name.Equals(context.Recipe, StringComparison.OrdinalIgnoreCase)));

            return executionId;
        }
    }
}