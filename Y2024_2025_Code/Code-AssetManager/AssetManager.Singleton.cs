using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Manager
{
    public partial class AssetManager
    {
        public static GameObject GetOrInstantiateSingleton(
            string key,
            Transform parent = null,
            bool instantiateInWorldSpace = false,
            bool trackHandle = true,
            AssetCategory assetCategory = AssetCategory.GlobalAssets)
        {
            return Instance.GetOrInstantiateSingletonInternal(key, parent, instantiateInWorldSpace, trackHandle, assetCategory);
        }

        public static UniTask<GameObject> GetOrInstantiateSingletonAsync(
            string key,
            Action<GameObject> successCallback = null,
            Transform parent = null,
            CancellationToken cancellationToken = default,
            bool instantiateInWorldSpace = false,
            bool trackHandle = true,
            AssetCategory assetCategory = AssetCategory.GlobalAssets)
        {
            return Instance.GetOrInstantiateSingletonAsyncInternal(key, successCallback, parent, cancellationToken, instantiateInWorldSpace, trackHandle, assetCategory);
        }

        private GameObject GetOrInstantiateSingletonInternal(
            string key,
            Transform parent,
            bool instantiateInWorldSpace,
            bool trackHandle,
            AssetCategory assetCategory)
        {
            AsyncOperationHandle<GameObject> handle = default;
            try
            {
                if (SingletonAssetHandleDict[assetCategory].TryGetValue(key, out var singletonHandle))
                {
                    if (singletonHandle.IsValid())
                    {
                        return singletonHandle.Result as GameObject;
                    }
                    else
                    {
                        SingletonAssetHandleDict[assetCategory].Remove(key);
                    }
                }
                handle = Addressables.InstantiateAsync(key, parent, instantiateInWorldSpace, trackHandle);
                handle.WaitForCompletion();

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    _singletonAssetHandleDict[assetCategory].Add(key, handle);
                    return handle.Result;
                }
            }
            finally
            {
                if (handle.IsValid() && handle.Status == AsyncOperationStatus.Failed)
                {
                    Addressables.ReleaseInstance(handle);
                }
            }

            return null;
        }

        private async UniTask<GameObject> GetOrInstantiateSingletonAsyncInternal(
            string key,
            Action<GameObject> successCallback,
            Transform parent,
            CancellationToken cancellationToken,
            bool instantiateInWorldSpace,
            bool trackHandle,
            AssetCategory assetCategory)
        {
            AsyncOperationHandle<GameObject> handle = default;
            try
            {
                if (SingletonAssetHandleDict[assetCategory].TryGetValue(key, out var singletonHandle))
                {
                    if (singletonHandle.IsValid())
                    {
                        successCallback?.Invoke(singletonHandle.Result as GameObject);
                        return singletonHandle.Result as GameObject;
                    }
                    else
                    {
                        SingletonAssetHandleDict[assetCategory].Remove(key);
                    }
                }

                handle = Addressables.InstantiateAsync(key, parent, instantiateInWorldSpace, trackHandle);
                await handle.Task.WithCancellation(cancellationToken);

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    SingletonAssetHandleDict[assetCategory].Add(key, handle);
                    successCallback?.Invoke(handle.Result);
                    return handle.Result;
                }
            }
            finally
            {
                if (handle.IsValid() && handle.Status == AsyncOperationStatus.Failed)
                {
                    Addressables.ReleaseInstance(handle);
                }
            }

            return null;
        }
    }
}