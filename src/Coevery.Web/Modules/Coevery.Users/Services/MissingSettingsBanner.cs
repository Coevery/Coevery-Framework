using System.Collections.Generic;
using Coevery.ContentManagement;
using Coevery.Localization;
using Coevery.Messaging.Services;
using Coevery.UI.Admin.Notification;
using Coevery.UI.Notify;
using Coevery.Users.Models;

namespace Coevery.Users.Services {
    public class MissingSettingsBanner : INotificationProvider {
        private readonly ICoeveryServices _CoeveryServices;
        private readonly IMessageChannelManager _messageManager;

        public MissingSettingsBanner(ICoeveryServices CoeveryServices, IMessageChannelManager messageManager) {
            _CoeveryServices = CoeveryServices;
            _messageManager = messageManager;
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        public IEnumerable<NotifyEntry> GetNotifications() {

            var registrationSettings = _CoeveryServices.WorkContext.CurrentSite.As<RegistrationSettingsPart>();

            if ( registrationSettings != null &&
                    ( registrationSettings.UsersMustValidateEmail ||
                    registrationSettings.NotifyModeration ||
                    registrationSettings.EnableLostPassword ) &&
                null == _messageManager.GetMessageChannel("Email", new Dictionary<string, object> {
                    {"Body", ""}, 
                    {"Subject", "Subject"},
                    {"Recipients", "john.doe@outlook.com"}
                }) ) {
                yield return new NotifyEntry { Message = T("Some Coevery.User settings require an Email channel to be enabled."), Type = NotifyType.Warning };
            }
        }
    }
}
