﻿using System.Reflection;
using System.Web.Compilation;

namespace Coevery.Environment.Extensions.Compilers {
    public class CSharpExtensionBuildProviderShim : BuildProvider, IShim {
        private readonly CompilerType _codeCompilerType;

        public CSharpExtensionBuildProviderShim() {
            CoeveryHostContainerRegistry.RegisterShim(this);

            _codeCompilerType = GetDefaultCompilerTypeForLanguage("C#");

            // NOTE: This code could be used to define a compilation flag with the current Coevery version 
            // but it's not compatible with Medium Trust
            var CoeveryVersion = new AssemblyName(typeof(IDependency).Assembly.FullName).Version;
            _codeCompilerType.CompilerParameters.CompilerOptions += string.Format("/define:Coevery_{0}_{1}", CoeveryVersion.Major, CoeveryVersion.Minor);
        }

        public ICoeveryHostContainer HostContainer { get; set; }

        public override CompilerType CodeCompilerType {
            get {
                return _codeCompilerType;
            }
        }

        public override void GenerateCode(AssemblyBuilder assemblyBuilder) {
            var context = new CompileExtensionContext {
                VirtualPath = this.VirtualPath,
                AssemblyBuilder =  new AspNetAssemblyBuilder(assemblyBuilder, this)
            };
            HostContainer.Resolve<IExtensionCompiler>().Compile(context);
        }
    }
}