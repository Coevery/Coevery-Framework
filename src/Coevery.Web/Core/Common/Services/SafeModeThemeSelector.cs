using System.Web.Routing;
using Coevery.Themes;
using JetBrains.Annotations;

namespace Coevery.Core.Common.Services {
    [UsedImplicitly]
    public class SafeModeThemeSelector : IThemeSelector {
        public ThemeSelectorResult GetTheme(RequestContext context) {
            return new ThemeSelectorResult {Priority = -100, ThemeName = "Themes"};
        }
    }
}