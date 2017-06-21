using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SiliconStudio.Assets.Compiler;
using SiliconStudio.BuildEngine;

namespace SiliconStudio.Assets.Tests.Compilers
{
    public class TestAssertCompiler<T> : TestCompilerBase where T : Asset
    {
        private class AssertCommand : AssetCommand<T>
        {
            private readonly AssetItem assetItem;
            private readonly Action<string, T, Package> assertFunc;

            public AssertCommand(AssetItem assetItem, T parameters, Package package, Action<string, T, Package> assertFunc)
                : base(assetItem.Location, parameters, package)
            {
                this.assetItem = assetItem;
                this.assertFunc = assertFunc;
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                assertFunc?.Invoke(Url, Parameters, Package);
                CompiledAssets.Add(assetItem);
                return Task.FromResult(ResultStatus.Successful);
            }
        }

        public override AssetCompilerResult Prepare(AssetCompilerContext context, AssetItem assetItem)
        {
            return new AssetCompilerResult(GetType().Name)
            {
                BuildSteps = new AssetBuildStep(assetItem) { new AssertCommand(assetItem, (T)assetItem.Asset, assetItem.Package, DoCommandAssert) }
            };
        }

        protected virtual void DoCommandAssert(string url, T parameters, Package package)
        {

        }
    }
}