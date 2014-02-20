using System.Collections.Generic;
using Coevery.Events;

namespace Coevery.Messaging.Services {
    public interface IMessageService : IEventHandler {
        void Send(string type, IDictionary<string, object> parameters);
    }
}