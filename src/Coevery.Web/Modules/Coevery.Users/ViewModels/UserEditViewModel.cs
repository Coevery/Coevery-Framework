using System.ComponentModel.DataAnnotations;
using Coevery.ContentManagement;
using Coevery.Users.Models;

namespace Coevery.Users.ViewModels {
    public class UserEditViewModel  {
        [Required]
        public string UserName {
            get { return User.As<UserPart>().UserName; }
            set { User.As<UserPart>().UserName = value; }
        }

        [Required]
        public string Email {
            get { return User.As<UserPart>().Email; }
            set { User.As<UserPart>().Email = value; }
        }

        public IContent User { get; set; }
    }
}