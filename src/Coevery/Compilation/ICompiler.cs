using System.Collections.Generic;

namespace Coevery.Compilation {
    public interface ICompiler : IDependency {
        object Compile(string code, IDictionary<string, object> parameters);
        T Compile<T>(string code, IDictionary<string, object> parameters);
    }
}