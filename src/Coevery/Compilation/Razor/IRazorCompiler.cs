using System;
using System.Collections.Generic;

namespace Coevery.Compilation.Razor {
    public interface IRazorCompiler : ICompiler {
        IRazorTemplateBase<TModel> CompileRazor<TModel>(string code, string name, IDictionary<string, object> parameters);
        IRazorTemplateBase CompileRazor(string code, string name, IDictionary<string, object> parameters);
    }
}