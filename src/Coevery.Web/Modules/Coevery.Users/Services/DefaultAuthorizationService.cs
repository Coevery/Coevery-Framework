using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Coevery.Localization;
using Coevery.Logging;
using Coevery.ContentManagement;
using Coevery.Security;
using Coevery.Security.Permissions;

namespace Coevery.Users.Services {
    [UsedImplicitly]
    public class DefaultAuthorizationService : IAuthorizationService {
        public void CheckAccess(Permission permission, IUser user, IContent content) {}

        public bool TryCheckAccess(Permission permission, IUser user, IContent content) {
            return true;
        }
    }
}
