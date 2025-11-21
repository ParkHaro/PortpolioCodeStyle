using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace Manager
{
    public partial class AssetManager
    {
        public static GameObject Instantiate(
            string key,
            string poolName = DefaultPoolName,
            Transform parent = null,
            bool instantiateInWorldSpace = false,
            bool trackHandle = true,
            AssetCategory assetCategory = AssetCategory.None)
        {
            return Instance.InstantiateInternal(key, poolName, parent, assetCategory);
        }

        public static UniTask<GameObject> InstantiateAsync(
            string key,
            string poolName = DefaultPoolName,
            Transform parent = null,
            CancellationToken cancellationToken = default,
            bool instantiateInWorldSpace = false,
            bool trackHandle = true,
            AssetCategory assetCategory = AssetCategory.None)
        {
            return Instance.InstantiateAsyncInternal(key, null, poolName, parent, cancellationToken, assetCategory);
        }

        public static UniTask<GameObject> InstantiateAsync(
            string key,
            Action<GameObject> successCallback,
            string poolName = DefaultPoolName,
            Transform parent = null,
            CancellationToken cancellationToken = default,
            bool instantiateInWorldSpace = false,
            bool trackHandle = true,
            AssetCategory assetCategory = AssetCategory.None)
        {
            return Instance.InstantiateAsyncInternal(key, successCallback, poolName, parent, cancellationToken, assetCategory);
        }

        private GameObject InstantiateInternal(
            string key,
            string poolName,
            Transform parent,
            AssetCategory assetCategory)
        {
            GameObject targetObject = null;
            try
            {
                var prefab = LoadAssetInternal<GameObject>(key, poolName, assetCategory);
                if (prefab == null)
                {
                    return null;
                }

                targetObject = UnmanagedInstantiate(prefab, parent);

                var assetPool = AssetPoolHubDict[GetCurrentTargetAssetCategory(assetCategory)].GetPool(poolName);
                assetPool.AddInstantiatedAssetObject(key, targetObject);
            }
            catch (Exception e)
            {
                DebugHelper.Log(e.ToString());
            }

            return targetObject;
        }

        private async UniTask<GameObject> InstantiateAsyncInternal(
            string key,
            Action<GameObject> successCallback,
            string poolName,
            Transform parent,
            CancellationToken cancellationToken,
            AssetCategory assetCategory)
        {
            GameObject prefab = null;
            GameObject targetObject = null;
            try
            {
                await LoadAssetAsyncInternal<GameObject>(key, result => { prefab = result; },
                    poolName, cancellationToken, assetCategory);
                if (prefab == null)
                {
                    return null;
                }

                targetObject = UnmanagedInstantiate(prefab, parent);

                var assetPool = AssetPoolHubDict[GetCurrentTargetAssetCategory(assetCategory)].GetPool(poolName);
                assetPool.AddInstantiatedAssetObject(key, targetObject);
                successCallback?.Invoke(targetObject);
            }
            catch (Exception e)
            {
                DebugHelper.LogError(e.ToString());
            }

            return targetObject;
        }

        public static bool ReleaseInstance(GameObject instance, string poolName = DefaultPoolName, AssetCategory assetCategory = AssetCategory.None)
        {
            if (instance == null)
            {
                return false;
            }

            var targetAssetCategory = GetCurrentTargetAssetCategory(assetCategory);
            if (targetAssetCategory == AssetCategory.None)
            {
                targetAssetCategory = GetBeforeTargetAssetCategory(assetCategory);
            }

            var assetPool = Instance.AssetPoolHubDict[targetAssetCategory].GetPool(poolName);
            if (assetPool == null)
            {
                return false;
            }

            assetPool.InstantiatedAssetObjectKeyDict.TryGetValue(instance, out var key);
            if (key == null)
            {
                return false;
            }

            if (!assetPool.InstantiatedAssetObjectDict.ContainsKey(key))
            {
                return false;
            }

            assetPool.RemoveInstantiatedAssetObject(instance);
            UnmanagedDestroy(instance);
            ReleaseLoadedAsset(key, poolName, targetAssetCategory);
            return true;
        }
    }
}