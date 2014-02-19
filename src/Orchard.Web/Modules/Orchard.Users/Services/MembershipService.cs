using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Security;
using JetBrains.Annotations;
using Orchard.Data;
using Orchard.DisplayManagement;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.ContentManagement;
using Orchard.Security;
using Orchard.Users.Events;
using Orchard.Users.Models;
using Orchard.Messaging.Services;
using System.Collections.Generic;
using Orchard.Services;

namespace Orchard.Users.Services {
    [UsedImplicitly]
    public class MembershipService : IMembershipService {
        private readonly IOrchardServices _orchardServices;
        private readonly IMessageService _messageService;
        private readonly IEnumerable<IUserEventHandler> _userEventHandlers;
        private readonly IEncryptionService _encryptionService;
        private readonly IShapeFactory _shapeFactory;
        private readonly IShapeDisplay _shapeDisplay;
        private readonly IRepository<UserRecord> _userRecordRepository;

        public MembershipService(
            IOrchardServices orchardServices,
            IMessageService messageService,
            IEnumerable<IUserEventHandler> userEventHandlers,
            IClock clock,
            IEncryptionService encryptionService,
            IShapeFactory shapeFactory,
            IShapeDisplay shapeDisplay, 
            IRepository<UserRecord> userRecordRepository) {
            _orchardServices = orchardServices;
            _messageService = messageService;
            _userEventHandlers = userEventHandlers;
            _encryptionService = encryptionService;
            _shapeFactory = shapeFactory;
            _shapeDisplay = shapeDisplay;
            _userRecordRepository = userRecordRepository;
            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }

        public MembershipSettings GetSettings() {
            var settings = new MembershipSettings();
            // accepting defaults
            return settings;
        }

        public IUser CreateUser(CreateUserParams createUserParams) {
            Logger.Information("CreateUser {0} {1}", createUserParams.Username, createUserParams.Email);

            var registrationSettings = _orchardServices.WorkContext.CurrentSite.As<RegistrationSettingsPart>();

            var user = new UserRecord();

            user.UserName = createUserParams.Username;
            user.Email = createUserParams.Email;
            user.NormalizedUserName = createUserParams.Username.ToLowerInvariant();
            user.HashAlgorithm = "SHA1";
            SetPassword(user, createUserParams.Password);

            if (registrationSettings != null) {
                user.RegistrationStatus = registrationSettings.UsersAreModerated ? UserStatus.Pending : UserStatus.Approved;
                user.EmailStatus = registrationSettings.UsersMustValidateEmail ? UserStatus.Pending : UserStatus.Approved;
            }

            if (createUserParams.IsApproved) {
                user.RegistrationStatus = UserStatus.Approved;
                user.EmailStatus = UserStatus.Approved;
            }

            var userContext = new UserContext {User = user, Cancel = false, UserParameters = createUserParams};
            foreach (var userEventHandler in _userEventHandlers) {
                userEventHandler.Creating(userContext);
            }

            if (userContext.Cancel) {
                return null;
            }

            _userRecordRepository.Create(user);

            foreach (var userEventHandler in _userEventHandlers) {
                userEventHandler.Created(userContext);
                if (user.RegistrationStatus == UserStatus.Approved) {
                    userEventHandler.Approved(user);
                }
            }

            if (registrationSettings != null
                && registrationSettings.UsersAreModerated
                && registrationSettings.NotifyModeration
                && !createUserParams.IsApproved) {
                var usernames = String.IsNullOrWhiteSpace(registrationSettings.NotificationsRecipients)
                    ? new string[0]
                    : registrationSettings.NotificationsRecipients.Split(new[] {',', ' '}, StringSplitOptions.RemoveEmptyEntries);

                foreach (var userName in usernames) {
                    if (String.IsNullOrWhiteSpace(userName)) {
                        continue;
                    }
                    var recipient = GetUser(userName);
                    if (recipient != null) {
                        var template = _shapeFactory.Create("Template_User_Moderated", Arguments.From(createUserParams));
                        template.Metadata.Wrappers.Add("Template_User_Wrapper");

                        var parameters = new Dictionary<string, object> {
                            {"Subject", T("New account").Text},
                            {"Body", _shapeDisplay.Display(template)},
                            {"Recipients", new[] {recipient.Email}}
                        };

                        _messageService.Send("Email", parameters);
                    }
                }
            }

            return user;
        }

        public IUser GetUser(string username) {
            var lowerName = username == null ? "" : username.ToLowerInvariant();

            return _userRecordRepository.Table.Where(u => u.NormalizedUserName == lowerName).ToList().FirstOrDefault();
        }

        public IUser ValidateUser(string userNameOrEmail, string password) {
            var lowerName = userNameOrEmail == null ? "" : userNameOrEmail.ToLowerInvariant();

            var user = _userRecordRepository.Table.Where(u => u.NormalizedUserName == lowerName).ToList().FirstOrDefault();

            if (user == null)
                user = _userRecordRepository.Table.Where(u => u.Email == lowerName).ToList().FirstOrDefault();

            if (user == null || ValidatePassword(user, password) == false)
                return null;

            if (user.EmailStatus != UserStatus.Approved)
                return null;

            if (user.RegistrationStatus != UserStatus.Approved)
                return null;

            return user;
        }

        public void SetPassword(IUser user, string password) {
            if (!(user is UserRecord))
                throw new InvalidCastException();

            var useRecord = user as UserRecord;

            switch (GetSettings().PasswordFormat) {
                case MembershipPasswordFormat.Clear:
                    SetPasswordClear(useRecord, password);
                    break;
                case MembershipPasswordFormat.Hashed:
                    SetPasswordHashed(useRecord, password);
                    break;
                case MembershipPasswordFormat.Encrypted:
                    SetPasswordEncrypted(useRecord, password);
                    break;
                default:
                    throw new ApplicationException(T("Unexpected password format value").ToString());
            }
        }

        private bool ValidatePassword(UserRecord userRecord, string password) {
            // Note - the password format stored with the record is used
            // otherwise changing the password format on the site would invalidate
            // all logins
            switch (userRecord.PasswordFormat) {
                case MembershipPasswordFormat.Clear:
                    return ValidatePasswordClear(userRecord, password);
                case MembershipPasswordFormat.Hashed:
                    return ValidatePasswordHashed(userRecord, password);
                case MembershipPasswordFormat.Encrypted:
                    return ValidatePasswordEncrypted(userRecord, password);
                default:
                    throw new ApplicationException("Unexpected password format value");
            }
        }

        private static void SetPasswordClear(UserRecord userRecord, string password) {
            userRecord.PasswordFormat = MembershipPasswordFormat.Clear;
            userRecord.Password = password;
            userRecord.PasswordSalt = null;
        }

        private static bool ValidatePasswordClear(UserRecord userRecord, string password) {
            return userRecord.Password == password;
        }

        private static void SetPasswordHashed(UserRecord userRecord, string password) {

            var saltBytes = new byte[0x10];
            using (var random = new RNGCryptoServiceProvider()) {
                random.GetBytes(saltBytes);
            }

            var passwordBytes = Encoding.Unicode.GetBytes(password);

            var combinedBytes = saltBytes.Concat(passwordBytes).ToArray();

            byte[] hashBytes;
            using (var hashAlgorithm = HashAlgorithm.Create(userRecord.HashAlgorithm)) {
                hashBytes = hashAlgorithm.ComputeHash(combinedBytes);
            }

            userRecord.PasswordFormat = MembershipPasswordFormat.Hashed;
            userRecord.Password = Convert.ToBase64String(hashBytes);
            userRecord.PasswordSalt = Convert.ToBase64String(saltBytes);
        }

        private static bool ValidatePasswordHashed(UserRecord userRecord, string password) {

            var saltBytes = Convert.FromBase64String(userRecord.PasswordSalt);

            var passwordBytes = Encoding.Unicode.GetBytes(password);

            var combinedBytes = saltBytes.Concat(passwordBytes).ToArray();

            byte[] hashBytes;
            using (var hashAlgorithm = HashAlgorithm.Create(userRecord.HashAlgorithm)) {
                hashBytes = hashAlgorithm.ComputeHash(combinedBytes);
            }

            return userRecord.Password == Convert.ToBase64String(hashBytes);
        }

        private void SetPasswordEncrypted(UserRecord userRecord, string password) {
            userRecord.Password = Convert.ToBase64String(_encryptionService.Encode(Encoding.UTF8.GetBytes(password)));
            userRecord.PasswordSalt = null;
            userRecord.PasswordFormat = MembershipPasswordFormat.Encrypted;
        }

        private bool ValidatePasswordEncrypted(UserRecord userRecord, string password) {
            return String.Equals(password, Encoding.UTF8.GetString(_encryptionService.Decode(Convert.FromBase64String(userRecord.Password))), StringComparison.Ordinal);
        }
    }
}
