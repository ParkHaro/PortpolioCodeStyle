using System;
using System.Collections.Generic;

namespace Manager
{
    public partial class AssetManager
    {
        private readonly Dictionary<string, IgnoreReleaseAssetContainer> _ignoreReleaseAssetContainerDict = new();
        private readonly Dictionary<string, string> _ignoreSoundPoolNameDict = new();

        public static bool IsIgnoreSound(string path)
        {
            return Instance.IsIgnoreSoundInternal(path);
        }

        private bool IsIgnoreSoundInternal(string path)
        {
            foreach (var container in _ignoreReleaseAssetContainerDict.Values)
            {
                _ignoreSoundPoolNameDict.TryGetValue(path, out var poolName);
                if (container.IsIgnore(poolName))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsIgnorePool(string poolName)
        {
            foreach (var container in _ignoreReleaseAssetContainerDict.Values)
            {
                if (container.IsIgnore(poolName))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IgnoreContainerContains(string containerName)
        {
            return _ignoreReleaseAssetContainerDict.ContainsKey(containerName);
        }

        private IgnoreReleaseAssetContainer GetIgnoreReleaseAssetContainerInstance(string containerName)
        {
            return _ignoreReleaseAssetContainerDict.GetValueOrDefault(containerName);
        }

        public static IgnoreReleaseAssetContainer GetIgnoreReleaseAssetContainer(string containerName)
        {
            return Instance.GetIgnoreReleaseAssetContainerInstance(containerName);
        }

        public static IgnoreReleaseAssetContainer GetOrCreateIgnoreReleaseAssetContainer(string containerName, HashSet<string> poolNameList)
        {
            if (Instance.IgnoreContainerContains(containerName))
            {
                return Instance.GetIgnoreReleaseAssetContainerInstance(containerName);
            }

            var assetContainer = new IgnoreReleaseAssetContainer(containerName, poolNameList);
            Instance._ignoreReleaseAssetContainerDict.Add(containerName, assetContainer);
            return assetContainer;
        }

        public static IgnoreReleaseAssetContainer RemoveIgnoreReleaseAssetContainer(string containerName)
        {
            if (!Instance.IgnoreContainerContains(containerName))
            {
                return null;
            }

            var assetContainer = GetIgnoreReleaseAssetContainer(containerName);
            assetContainer.Dispose();
            return assetContainer;
        }

        public class IgnoreReleaseAssetContainer : IDisposable
        {
            private readonly string _containerName;
            private AssetCategory _currentAssetCategory;
            private readonly HashSet<string> _poolNameSet;
            private bool _isLoaded;
            private bool _isValid;

            public string ContainerName => _containerName;
            public AssetCategory CurrentAssetCategory => _currentAssetCategory;

            public IgnoreReleaseAssetContainer(string containerName, HashSet<string> poolNameSet)
            {
                _containerName = containerName;
                _currentAssetCategory = GetCurrentTargetAssetCategory(AssetCategory.None);
                _poolNameSet = poolNameSet;

                _isValid = true;
            }

            public void Dispose()
            {
                if (!_isValid)
                {
                    PrintLogInvalidContainer();
                    return;
                }

                _poolNameSet.Clear();

                _isValid = false;
            }

            private void PrintLogInvalidContainer()
            {
                DebugHelper.LogWarning("Invalid container.");
            }

            public bool IsIgnore(string poolName)
            {
                return _poolNameSet.Contains(poolName);
            }

            public IgnoreReleaseAssetContainer ReleaseAssets()
            {
                Instance.ReleaseAssets(_poolNameSet);
                return this;
            }
        }
    }
}