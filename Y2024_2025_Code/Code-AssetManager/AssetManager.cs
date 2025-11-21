using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Manager
{
    public partial class AssetManager : MonoSingletonDontDestroyed<AssetManager>
    {
        public const string DefaultPoolName = "Default";
        private const string AssetPath = "Assets/Bundles/";
        private const float CheckInterval = 5f;
        private float _checkTimer = 0f;

        [ShowInInspector] private readonly SerializableDictionary<AssetCategory, AssetPoolHub> _assetPoolHubDict = new();
        private IReadOnlyDictionary<AssetCategory, AssetPoolHub> AssetPoolHubDict
        {
            get
            {
                if (_assetPoolHubDict.Count == 0)
                {
                    Init();
                }

                return _assetPoolHubDict;
            }
        }

        public IReadOnlyDictionary<AssetCategory, AssetPoolHub> GetAssetPoolHubDict()
        {
            return AssetPoolHubDict;
        }

        private readonly Dictionary<AssetCategory, Dictionary<string, AsyncOperationHandle>> _singletonAssetHandleDict = new();
        private IReadOnlyDictionary<AssetCategory, Dictionary<string, AsyncOperationHandle>> SingletonAssetHandleDict
        {
            get
            {
                if (_singletonAssetHandleDict.Count == 0)
                {
                    Init();
                }

                return _singletonAssetHandleDict;
            }
        }

        [ShowInInspector] private readonly SerializableDictionary<AssetCategory, ObjectPoolHub> _objectPoolHubDict = new();
        private IReadOnlyDictionary<AssetCategory, ObjectPoolHub> ObjectPoolHubDict
        {
            get
            {
                if (_objectPoolHubDict.Count == 0)
                {
                    Init();
                }

                return _objectPoolHubDict;
            }
        }
        public AssetCategory CurrentAssetCategory =>
            GameModeManager.Instance.CurrentContext != null && GameModeManager.Instance.CurrentContext.GameMode != null
                ? GameModeManager.Instance.CurrentContext.GameMode.CleanupAssetCategory
                : AssetCategory.None;

        private Dictionary<string, bool> _pathExistDict = new();

        // ReSharper disable once UnusedMember.Local
        private string GetPlatformString()
        {
#if UNITY_ANDROID
            return "Android";
#elif UNITY_IOS
        return "iOS";
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return "Android";
#else
        return "Android";
#endif
        }

        private void Init()
        {
            var enumValues = typeof(AssetCategory).GetEnumValues();
            for (int i = 0; i < enumValues.Length; i++)
            {
                _assetPoolHubDict.Add((AssetCategory)i, new());
            }

            for (int i = 0; i < enumValues.Length; i++)
            {
                _singletonAssetHandleDict.Add((AssetCategory)i, new());
            }

            for (int i = 0; i < enumValues.Length; i++)
            {
                _objectPoolHubDict.Add((AssetCategory)i, new());
            }

            _pathExistDict = new();
        }

        public override void OnDestroy()
        {
            ReleaseAllHubs();
            ReleaseAllIgnoreReleaseAssetContainers();

            _pathExistDict.Clear();
            base.OnDestroy();
        }
        
        private void Update()
        {
            _checkTimer += Time.unscaledDeltaTime;
            if (_checkTimer >= CheckInterval)
            {
                _checkTimer = 0f;
                foreach (var poolHub in _assetPoolHubDict.Values)
                {
                    if (poolHub == null)
                        continue;
                    
                    foreach (var pool in poolHub.PoolDict.Values)
                    {
                        pool?.CleanInvalidInstances();
                    }
                }
            }
        }

        public static bool Exists(string pathToAsset)
        {
            return Instance.ExistsInternal(pathToAsset);
        }

        private bool ExistsInternal(string pathToAsset)
        {
            if (_pathExistDict.TryGetValue(pathToAsset, out var isExist))
            {
                return isExist;
            }

            AsyncOperationHandle<IList<IResourceLocation>> resourceLocation = default;

            try
            {
                resourceLocation = Addressables.LoadResourceLocationsAsync(pathToAsset);
                resourceLocation.WaitForCompletion();

                var locations = resourceLocation.Task;
                isExist = locations != null && locations.Result.Count > 0;
                return isExist;
            }
            catch (Exception e)
            {
                DebugHelper.LogWarning($"Error loading resource locations for {pathToAsset}: {e.Message}");
                return false;
            }
            finally
            {
                if (resourceLocation.IsValid())
                {
                    Addressables.Release(resourceLocation);
                }

                _pathExistDict.Add(pathToAsset, isExist);
            }
        }

        public void ReleaseAllHubs()
        {
            foreach (var assetPoolHub in _assetPoolHubDict.Values)
            {
                assetPoolHub.Dispose();
            }

            foreach (var objectPoolHub in _objectPoolHubDict.Values)
            {
                objectPoolHub.Dispose();
            }

            foreach (var singletonAssetDict in _singletonAssetHandleDict.Values)
            {
                foreach (var asyncOperationHandle in singletonAssetDict.Values)
                {
                    if (asyncOperationHandle.IsValid() && asyncOperationHandle.Result != null)
                    {
                        Addressables.ReleaseInstance(asyncOperationHandle.Result as GameObject);
                    }
                }

                singletonAssetDict.Clear();
            }

            _assetPoolHubDict.Clear();
            _singletonAssetHandleDict.Clear();
            _objectPoolHubDict.Clear();
        }

        public void ReleaseAllIgnoreReleaseAssetContainers()
        {
            var removeList = _ignoreReleaseAssetContainerDict.Values.ToList();
            foreach (var managedAssetContainer in removeList)
            {
                managedAssetContainer.Dispose();
            }

            _ignoreReleaseAssetContainerDict.Clear();
        }

        [Button]
        public void CleanupObjectPool()
        {
            ObjectPoolHubDict[AssetCategory.InGameAssets].Dispose();
        }

        [Button]
        public void Cleanup(AssetCategory assetCategory)
        {
            AssetPoolHubDict[assetCategory].Dispose();
            ObjectPoolHubDict[assetCategory].Dispose();
            foreach (var asyncOperationHandle in SingletonAssetHandleDict[assetCategory].Values)
            {
                if (asyncOperationHandle.IsValid() && asyncOperationHandle.Result != null)
                {
                    Addressables.ReleaseInstance(asyncOperationHandle.Result as GameObject);
                }
            }

            SingletonAssetHandleDict[assetCategory].Clear();
            Provider.ReleaseLoadedProviderAssets();
        }

        private static AssetCategory GetCurrentTargetAssetCategory(AssetCategory assetCategory)
        {
            var targetAssetCategory = assetCategory;
            if (assetCategory == AssetCategory.None
                && GameModeManager.Instance.CurrentContext != null
                && GameModeManager.Instance.CurrentContext.GameMode != null)
            {
                var cleanupAssetCategory = GameModeManager.Instance.CurrentContext.GameMode.CleanupAssetCategory;
                targetAssetCategory = cleanupAssetCategory != AssetCategory.None ? cleanupAssetCategory : assetCategory;
            }

            return targetAssetCategory;
        }

        private static AssetCategory GetBeforeTargetAssetCategory(AssetCategory assetCategory)
        {
            var targetAssetCategory = assetCategory;
            if (assetCategory == AssetCategory.None && GameModeManager.Instance.BeforeAssetCategory != null)
            {
                var cleanupAssetCategory = GameModeManager.Instance.BeforeAssetCategory;
                targetAssetCategory = (AssetCategory)(cleanupAssetCategory != AssetCategory.None ? cleanupAssetCategory : assetCategory);
            }

            return targetAssetCategory;
        }
    }
}

public enum AssetCategory
{
    None,
    GlobalAssets,
    InGameAssets,
    OutGameAssets,
    KingdomAssets,
    CommonAssets,
}

public static class TaskExtensions
{
    public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>();
        await using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
        {
            if (task != await Task.WhenAny(task, tcs.Task))
            {
                DebugHelper.Log("WithCancellation Task Cancelled");
                throw new OperationCanceledException(cancellationToken);
            }
        }

        return await task;
    }
}