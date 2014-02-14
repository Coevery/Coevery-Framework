using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.ContentManagement;
using Orchard.Security;
using Orchard.Security.Permissions;

namespace Orchard.Roles.Services {
    [UsedImplicitly]
    public class DefaultAuthorizationService : IAuthorizationService {
        public void CheckAccess(Permission permission, IUser user, IContent content) {}

        public bool TryCheckAccess(Permission permission, IUser user, IContent content) {
            return true;
        }
    }
}
