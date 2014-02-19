using System.Collections.Generic;

namespace Coevery.Messaging.Services {
    public interface IMessageChannel : IDependency {
        void Process(IDictionary<string, object> parameters);
    }
}