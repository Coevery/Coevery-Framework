using System;
using System.Collections.Generic;
using System.Linq;
using Coevery.Environment.Features;
using Coevery.Localization;
using Coevery.UI.Notify;

namespace Coevery.Core.Settings.Services {

    public interface IModuleService : IDependency {
        /// <summary>
        /// Enables a list of features.
        /// </summary>
        /// <param name="featureIds">The IDs for the features to be enabled.</param>
        /// <param name="force">Boolean parameter indicating if the feature should enable it's dependencies if required or fail otherwise.</param>
        void EnableFeatures(IEnumerable<string> featureIds, bool force);

        /// <summary>
        /// Disables a list of features.
        /// </summary>
        /// <param name="featureIds">The IDs for the features to be disabled.</param>
        /// <param name="force">Boolean parameter indicating if the feature should disable the features which depend on it if required or fail otherwise.</param>
        void DisableFeatures(IEnumerable<string> featureIds, bool force);

    }

    public class ModuleService : IModuleService {
        private readonly IFeatureManager _featureManager;

        public ModuleService(
                IFeatureManager featureManager,
                ICoeveryServices CoeveryServices) {

            Services = CoeveryServices;

            _featureManager = featureManager;

            if (_featureManager.FeatureDependencyNotification == null) {
                _featureManager.FeatureDependencyNotification = GenerateWarning;
            }

            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }
        public ICoeveryServices Services { get; set; }

        /// <summary>
        /// Enables a list of features.
        /// </summary>
        /// <param name="featureIds">The IDs for the features to be enabled.</param>
        /// <param name="force">Boolean parameter indicating if the feature should enable it's dependencies if required or fail otherwise.</param>
        public void EnableFeatures(IEnumerable<string> featureIds, bool force) {
            foreach (string featureId in _featureManager.EnableFeatures(featureIds, force)) {
                var featureName = _featureManager.GetAvailableFeatures().First(f => f.Id.Equals(featureId, StringComparison.OrdinalIgnoreCase)).Name;
                Services.Notifier.Information(T("{0} was enabled", featureName));
            }
        }

        /// <summary>
        /// Disables a list of features.
        /// </summary>
        /// <param name="featureIds">The IDs for the features to be disabled.</param>
        /// <param name="force">Boolean parameter indicating if the feature should disable the features which depend on it if required or fail otherwise.</param>
        public void DisableFeatures(IEnumerable<string> featureIds, bool force) {
            foreach (string featureId in _featureManager.DisableFeatures(featureIds, force)) {
                var featureName = _featureManager.GetAvailableFeatures().Where(f => f.Id == featureId).First().Name;
                Services.Notifier.Information(T("{0} was disabled", featureName));
            }
        }

        private void GenerateWarning(string messageFormat, string featureName, IEnumerable<string> featuresInQuestion) {
            if (featuresInQuestion.Count() < 1)
                return;

            Services.Notifier.Warning(T(
                messageFormat,
                featureName,
                featuresInQuestion.Count() > 1
                    ? string.Join("",
                                  featuresInQuestion.Select(
                                      (fn, i) =>
                                      T(i == featuresInQuestion.Count() - 1
                                            ? "{0}"
                                            : (i == featuresInQuestion.Count() - 2
                                                   ? "{0} and "
                                                   : "{0}, "), fn).ToString()).ToArray())
                    : featuresInQuestion.First()));
        }
    }
}