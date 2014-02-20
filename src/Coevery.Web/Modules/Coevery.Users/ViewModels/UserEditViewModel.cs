using System.ComponentModel.DataAnnotations;
using Coevery.ContentManagement;
using Coevery.Security;
using Coevery.Users.Models;

namespace Coevery.Users.ViewModels {
    public class UserEditViewModel  {
        [Required]
        public string UserName {
            get { return User.UserName; }
            set { User.UserName = value; }
        }

        [Required]
        public string Email {
            get { return User.Email; }
            set { User.Email = value; }
        }

        public UserRecord User { get; set; }
    }
}