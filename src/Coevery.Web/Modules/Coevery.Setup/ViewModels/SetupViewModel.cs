using System.Collections.Generic;
using Coevery.Recipes.Models;
using Coevery.Setup.Annotations;
using Coevery.Setup.Controllers;

namespace Coevery.Setup.ViewModels {
    public class SetupViewModel  {
        public SetupViewModel() {
        }

        [SiteNameValid(maximumLength: 70)]
        public string SiteName { get; set; }
        public SetupDatabaseType DatabaseProvider { get; set; }
        
        public string DatabaseConnectionString { get; set; }
        public string DatabaseTablePrefix { get; set; }
        public bool DatabaseIsPreconfigured { get; set; }

        public IEnumerable<Recipe> Recipes { get; set; }
        public string Recipe { get; set; }
        public string RecipeDescription { get; set; }
    }
}