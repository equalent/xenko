// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using NUnit.Framework;
using SiliconStudio.Core;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Yaml;
using SiliconStudio.Core.Yaml.Serialization;

namespace SiliconStudio.Assets.Tests
{
    [TestFixture]
    public class TestAssetUpgrade : TestBase
    {
        [DataContract("MyUpgradedAsset")]
        [AssetDescription(".xkobj")]
        [AssetFormatVersion("TestPackage", 5, 1)]
        [AssetUpgrader("TestPackage", 1, 2, typeof(AssetUpgrader1))]
        [AssetUpgrader("TestPackage", 2, 4, typeof(AssetUpgrader2))]
        [AssetUpgrader("TestPackage", 4, 5, typeof(AssetUpgrader3))]
        public class MyUpgradedAsset : Asset
        {
            public MyUpgradedAsset(int version)
            {
                SerializedVersion["TestPackage"] = PackageVersion.Parse("0.0." + version);
            }

            public MyUpgradedAsset()
            {
            }

            public Vector3 Vector { get; set; }
            public List<int> Test1 { get; set; }
            public List<int> Test2 { get; set; }
            public List<int> Test3 { get; set; }
            public List<int> Test4 { get; set; }
            public List<int> Test5 { get; set; }

            class AssetUpgrader1 : IAssetUpgrader
            {
                public void Upgrade(AssetMigrationContext context, string dependencyName, PackageVersion currentVersion, PackageVersion targetVersion, YamlMappingNode yamlAssetNode, PackageLoadingAssetFile assetFile)
                {
                    dynamic asset = new DynamicYamlMapping(yamlAssetNode);

                    // Note: seems little bit strange, but original test was not using targetVersion...
                    var serializedVersion = AssetRegistry.GetCurrentFormatVersions(typeof(MyUpgradedAsset))[dependencyName];
                    AssetUpgraderBase.SetSerializableVersion(asset, dependencyName, serializedVersion);

                    // Move Test1 to Test2
                    asset.Test2 = asset.Test1;
                    asset.Test1 = DynamicYamlEmpty.Default;
                }
            }

            class AssetUpgrader2 : IAssetUpgrader
            {
                public void Upgrade(AssetMigrationContext context, string dependencyName, PackageVersion currentVersion, PackageVersion targetVersion, YamlMappingNode yamlAssetNode, PackageLoadingAssetFile assetFile)
                {
                    dynamic asset = new DynamicYamlMapping(yamlAssetNode);

                    AssetUpgraderBase.SetSerializableVersion(asset, dependencyName, targetVersion);

                    // Move Test2 to Test4
                    if (currentVersion == PackageVersion.Parse("0.0.2"))
                    {
                        asset.Test4 = asset.Test2;
                        asset.Test2 = DynamicYamlEmpty.Default;
                    }
                    // Move Test3 to Test4
                    else if (currentVersion == PackageVersion.Parse("0.0.3"))
                    {
                        asset.Test4 = asset.Test3;
                        asset.Test3 = DynamicYamlEmpty.Default;
                    }
                }
            }

            class AssetUpgrader3 : IAssetUpgrader
            {
                public void Upgrade(AssetMigrationContext context, string dependencyName, PackageVersion currentVersion, PackageVersion targetVersion, YamlMappingNode yamlAssetNode, PackageLoadingAssetFile assetFile)
                {
                    dynamic asset = new DynamicYamlMapping(yamlAssetNode);

                    AssetUpgraderBase.SetSerializableVersion(asset, dependencyName, targetVersion);

                    // Move Test4 to Test5
                    asset.Test5 = asset.Test4;
                    asset.Test4 = DynamicYamlEmpty.Default;
                }
            }
        }

        [Test]
        public void Version1()
        {
            var asset = new MyUpgradedAsset(1) { Vector = new Vector3(12.0f, 15.0f, 17.0f), Test1 = new List<int> { 32, 64 } };
            TestUpgrade(asset, true);
        }

        [Test]
        public void Version2()
        {
            var asset = new MyUpgradedAsset(2) { Vector = new Vector3(12.0f, 15.0f, 17.0f), Test2 = new List<int> { 32, 64 } };
            TestUpgrade(asset, true);
        }

        [Test]
        public void Version3()
        {
            var asset = new MyUpgradedAsset(3) { Vector = new Vector3(12.0f, 15.0f, 17.0f), Test3 = new List<int> { 32, 64 } };
            TestUpgrade(asset, true);
        }

        [Test]
        public void Version4()
        {
            var asset = new MyUpgradedAsset(4) { Vector = new Vector3(12.0f, 15.0f, 17.0f), Test4 = new List<int> { 32, 64 } };
            TestUpgrade(asset, true);
        }

        [Test]
        public void Version5()
        {
            var asset = new MyUpgradedAsset(5) { Vector = new Vector3(12.0f, 15.0f, 17.0f), Test5 = new List<int> { 32, 64 } };
            TestUpgrade(asset, false);
        }

        public void TestUpgrade(MyUpgradedAsset asset, bool needMigration)
        {
            var loadingFilePath = new PackageLoadingAssetFile(Path.Combine(DirectoryTestBase, "TestUpgrade\\Asset1.xkobj"), DirectoryTestBase);
            var outputFilePath = loadingFilePath.FilePath.FullPath;
            AssetFileSerializer.Save(outputFilePath, asset, null);

            var logger = new LoggerResult();
            var context = new AssetMigrationContext(null, loadingFilePath.ToReference(), loadingFilePath.FilePath.ToWindowsPath(), logger);
            Assert.AreEqual(AssetMigration.MigrateAssetIfNeeded(context, loadingFilePath, "TestPackage"), needMigration);

            if (needMigration)
            {
                using (var fileStream = new FileStream(outputFilePath, FileMode.Truncate))
                    fileStream.Write(loadingFilePath.AssetContent, 0, loadingFilePath.AssetContent.Length);
            }

            Console.WriteLine(File.ReadAllText(outputFilePath).Trim());

            var upgradedAsset = AssetFileSerializer.Load<MyUpgradedAsset>(outputFilePath).Asset;
            AssertUpgrade(upgradedAsset);
        }

        private static void AssertUpgrade(MyUpgradedAsset asset)
        {
            Assert.That(asset.SerializedVersion["TestPackage"], Is.EqualTo(new PackageVersion("0.0.5")));
            Assert.That(asset.Test1, Is.Null);
            Assert.That(asset.Test2, Is.Null);
            Assert.That(asset.Test3, Is.Null);
            Assert.That(asset.Test4, Is.Null);
            Assert.That(asset.Test5, Is.Not.Null);
        }
    }
}
