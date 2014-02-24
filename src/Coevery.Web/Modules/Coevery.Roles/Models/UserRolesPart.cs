using System.Collections.Generic;
using Coevery.ContentManagement;
using Coevery.ContentManagement.Utilities;

namespace Coevery.Roles.Models {
    public class UserRolesPart : ContentPart, IUserRoles {

        internal LazyField<IList<string>> _roles = new LazyField<IList<string>>();

        public IList<string> Roles {
            get { return _roles.Value; }
        }
    }
}