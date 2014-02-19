using System.Web.Security;
using Orchard.Security;

namespace Orchard.Users.Models {
    public class UserRecord : IUser {

        public const string EmailPattern = @"^(?![\.@])(""([^""\r\\]|\\[""\r\\])*""|([-a-z0-9!#$%&'*+/=?^_`{|}~]|(?<!\.)\.)*)(?<!\.)"
            + @"@([a-z0-9][\w-]*\.)+[a-z]{2,}$";

        public int Id { get; set; }
        public virtual string UserName { get; set; }
        public virtual string Email { get; set; }
        public virtual string NormalizedUserName { get; set; }

        public virtual string Password { get; set; }
        public virtual MembershipPasswordFormat PasswordFormat { get; set; }
        public virtual string HashAlgorithm { get; set; }
        public virtual string PasswordSalt { get; set; }

        public virtual UserStatus RegistrationStatus { get; set; }
        public virtual UserStatus EmailStatus { get; set; }
        public virtual string EmailChallengeToken { get; set; }

        public ContentManagement.ContentItem ContentItem
        {
            get { throw new System.NotImplementedException(); }
        }
    }
}