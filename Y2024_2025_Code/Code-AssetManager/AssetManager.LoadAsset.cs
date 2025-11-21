using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Manager
{
    public partial class AssetManager
    {
        public static T LoadAssetCommon<T>(string key, string poolName = DefaultPoolName)
            where T : Object
        {
            return Instance.LoadAssetInternal<T>(key, poolName, AssetCategory.CommonAssets);
        }

        public static T LoadAssetInGame<T>(string key, string poolName = DefaultPoolName)
            where T : Object
        {
            return Instance.LoadAssetInternal<T>(key, poolName, AssetCategory.InGameAssets);
        }

        public static T LoadAssetOutGame<T>(string key, string poolName = DefaultPoolName)
            where T : Object
        {
            return Instance.LoadAssetInternal<T>(key, poolName, AssetCategory.OutGameAssets);
        }

        public static T LoadAssetKingdom<T>(string key, string poolName = DefaultPoolName)
            where T : Object
        {
            return Instance.LoadAssetInternal<T>(key, poolName, AssetCategory.KingdomAssets);
        }

        public static T LoadAssetGlobal<T>(string key, string poolName = DefaultPoolName)
            where T : Object
        {
            return Instance.LoadAssetInternal<T>(key, poolName, AssetCategory.GlobalAssets);
        }

        public static GameObject LoadAsset(string key, string poolName = DefaultPoolName)
        {
            return Instance.LoadAssetInternal<GameObject>(key, poolName, AssetCategory.None);
        }

        public static T LoadAsset<T>(string key, string poolName = DefaultPoolName, AssetCategory assetCategory = AssetCategory.None)
            where T : Object
        {
            return Instance.LoadAssetInternal<T>(key, poolName, assetCategory);
        }

        public static Sprite LoadSpriteAsset(string key, string poolName = DefaultPoolName, AssetCategory assetCategory = AssetCategory.None)
        {
            var texture = Instance.LoadAssetInternal<Texture2D>(key, poolName, assetCategory);
            if (texture == null)
                return null;

            return Sprite.Create(texture, new(0, 0, texture.width, texture.height), new(0.5f, 0.5f));
        }

        public static async UniTask<Sprite> LoadSpriteAssetAsync(
            string key,
            string poolName = DefaultPoolName,
            CancellationToken cancellationToken = default,
            AssetCategory assetCategory = AssetCategory.None
        )
        {
            var loadTask = Instance.LoadAssetAsyncInternal<Texture2D>(key, null, poolName, cancellationToken, assetCategory);
            var awaiter = loadTask.GetAwaiter();
            await UniTask.WaitUntil(() => awaiter.IsCompleted, cancellationToken: cancellationToken);
            if (loadTask.Status != UniTaskStatus.Succeeded)
            {
                DebugHelper.Log($"Sprite Load Failed: {key}");
                return null;
            }

            var texture = awaiter.GetResult();
            if (texture == null)
            {
                return null;
            }

            return Sprite.Create(texture, new(0, 0, texture.width, texture.height), new(0.5f, 0.5f));
        }

        public static UniTask<T> LoadAssetCommonAsync<T>(
            string key,
            string poolName = DefaultPoolName,
            CancellationToken cancellationToken = default
        )
            where T : Object
        {
            return Instance.LoadAssetAsyncInternal<T>(key, null, poolName, cancellationToken, AssetCategory.CommonAssets);
        }

        public static UniTask<T> LoadAssetInGameAsync<T>(
            string key,
            string poolName = DefaultPoolName,
            CancellationToken cancellationToken = default
        )
            where T : Object
        {
            return Instance.LoadAssetAsyncInternal<T>(key, null, poolName, cancellationToken, AssetCategory.InGameAssets);
        }

        public static UniTask<T> LoadAssetOutGameAsync<T>(
            string key,
            string poolName = DefaultPoolName,
            CancellationToken cancellationToken = default
        )
            where T : Object
        {
            return Instance.LoadAssetAsyncInternal<T>(key, null, poolName, cancellationToken, AssetCategory.OutGameAssets);
        }

        public static UniTask<T> LoadAssetKingdomAsync<T>(
            string key,
            string poolName = DefaultPoolName,
            CancellationToken cancellationToken = default
        )
            where T : Object
        {
            return Instance.LoadAssetAsyncInternal<T>(key, null, poolName, cancellationToken, AssetCategory.KingdomAssets);
        }

        public static UniTask<T> LoadAssetGlobalAsync<T>(
            string key,
            string poolName = DefaultPoolName,
            CancellationToken cancellationToken = default
        )
            where T : Object
        {
            return Instance.LoadAssetAsyncInternal<T>(key, null, poolName, cancellationToken, AssetCategory.GlobalAssets);
        }

        public static UniTask<GameObject> LoadAssetAsync(string key, string poolName = DefaultPoolName, AssetCategory assetCategory = AssetCategory.None)
        {
            return Instance.LoadAssetAsyncInternal<GameObject>(key, null, poolName, default, assetCategory);
        }

        public static UniTask<T> LoadAssetAsync<T>(
            string key,
            string poolName = DefaultPoolName,
            AssetCategory assetCategory = AssetCategory.None,
            CancellationToken cancellationToken = default
        )
            where T : Object
        {
            return Instance.LoadAssetAsyncInternal<T>(key, null, poolName, cancellationToken, assetCategory);
        }

        public static UniTask<T> LoadAssetAsync<T>(
            string key,
            Action<T> successCallback,
            string poolName = DefaultPoolName,
            CancellationToken cancellationToken = default,
            AssetCategory assetCategory = AssetCategory.None
        )
            where T : Object
        {
            return Instance.LoadAssetAsyncInternal(key, successCallback, poolName, cancellationToken, assetCategory);
        }

        private T LoadAssetInternal<T>(
            string key,
            string poolName,
            AssetCategory assetCategory
        )
            where T : Object
        {
            AsyncOperationHandle handle = default;
            try
            {
                handle = GetExistAssetProcess<T>(key, null, poolName, assetCategory, out var result);
                if (handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded)
                {
                    return result;
                }

                if (!handle.IsValid())
                {
                    if (!Exists(key))
                    {
                        return null;
                    }

                    handle = Addressables.LoadAssetAsync<T>(key);
                }

                PrepareHandleProcess(key, poolName, assetCategory, handle);
                handle.WaitForCompletion();

                if (HandleSuccessProcess<T>(null, handle, out var handleResult))
                {
                    return handleResult;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed Load Asset: {key} \n {e}");
            }
            finally
            {
                if (handle.IsValid() && handle.Status == AsyncOperationStatus.Failed)
                {
                    Addressables.Release(handle);
                }
            }

            return null;
        }

        private async UniTask<T> LoadAssetAsyncInternal<T>(
            string key,
            Action<T> successCallback,
            string poolName,
            CancellationToken cancellationToken,
            AssetCategory assetCategory
        )
            where T : Object
        {
            AsyncOperationHandle handle = default;
            try
            {
                handle = GetExistAssetProcess(key, successCallback, poolName, assetCategory, out var result);
                if (handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded)
                {
                    return result;
                }

                if (!handle.IsValid())
                {
                    if (!Exists(key))
                    {
                        return null;
                    }

                    // Debug.LogWarning($"LoadAssetAsync : {key} / {poolName} / {assetCategory}");
                    handle = Addressables.LoadAssetAsync<T>(key);
                }

                PrepareHandleProcess(key, poolName, assetCategory, handle);
                await handle.Task.WithCancellation(cancellationToken);
                if (HandleSuccessProcess(successCallback, handle, out var handleResult))
                {
                    return handleResult;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed Load Asset: {key} \n {e}");
                successCallback?.Invoke(null);
            }
            finally
            {
                if (handle.IsValid() && handle.Status == AsyncOperationStatus.Failed)
                {
                    Addressables.Release(handle);
                }
            }

            return null;
        }

        private AsyncOperationHandle GetExistAssetProcess<T>(string key, Action<T> successCallback, string poolName, AssetCategory assetCategory, out T result)
            where T : Object
        {
            if (Exists(key))
            {
                var targetAssetCategory = GetCurrentTargetAssetCategory(assetCategory);

                var assetPoolHub = AssetPoolHubDict[targetAssetCategory];
                if (assetPoolHub.ContainsPool(poolName))
                {
                    var assetPool = assetPoolHub.GetPool(poolName);
                    var assetHandle = assetPool.GetAssetHandleWithAddCount(key);
                    if (assetHandle.IsValid() && assetHandle.Result != null)
                    {
                        successCallback?.Invoke(assetHandle.Result as T);
                        {
                            if (assetHandle.Result is Sprite sprite)
                            {
                                result = sprite.texture as T;
                            }
                            else
                            {
                                result = assetHandle.Result as T;
                            }

                            return assetHandle;
                        }
                    }

                    result = null;
                    return assetHandle;
                }
            }
            else
            {
                DebugHelper.LogWarning($"Not Exist Asset: {key}");
            }

            result = null;
            return default;
        }

        private void PrepareHandleProcess(string key, string poolName, AssetCategory assetCategory, AsyncOperationHandle handle)
        {
            var targetAssetCategory = GetCurrentTargetAssetCategory(assetCategory);

            var assetPoolHub = _assetPoolHubDict[targetAssetCategory];
            if (!assetPoolHub.ContainsPool(poolName))
            {
                AssetPoolHubDict[targetAssetCategory].AddPool(poolName);
            }

            assetPoolHub.AddLoadAsset(poolName, key, handle);
        }

        private bool HandleSuccessProcess<T>(
            Action<T> successCallback,
            AsyncOperationHandle handle,
            out T handleResult
        )
            where T : Object
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                successCallback?.Invoke(handle.Result as T);
                handleResult = handle.Result as T;
                return true;
            }

            handleResult = null;
            return false;
        }

        public static async UniTask LoadAssetTextAsync(string path, Action<string> callback)
        {
            string loadText;
            bool loadFile;
            TextAsset asset;

            string assetTextPath = SB.Str(AssetPath, path);
            asset = await LoadAssetTextAsync(assetTextPath);
            loadFile = true;

            await UniTask.WaitUntil(() => loadFile);
            loadText = asset?.text;

            if (loadText == string.Empty)
                callback?.Invoke(null);
            else
                callback?.Invoke(loadText);
        }

        public static async UniTask<TextAsset> LoadAssetTextAsync(string path)
        {
            return await LoadAssetAsync<TextAsset>(path);
        }

        public static void ReleaseLoadedAsset<TObject>(TObject obj, string poolName = DefaultPoolName, AssetCategory assetCategory = AssetCategory.None)
            where TObject : Object
        {
            if (obj == null)
            {
                return;
            }

            var targetAssetCategory = GetTargetAssetCategory(assetCategory);
            if (!Instance.AssetPoolHubDict[targetAssetCategory].ContainsPool(poolName))
            {
                return;
            }

            // Find the key associated with this object
            string key = null;

            foreach (var pair in Instance.AssetPoolHubDict[targetAssetCategory].GetPool(poolName).LoadedAssetHandleDict)
            {
                var loadedAsset = pair.Value;
                var assetHandle = loadedAsset.Handle;
                if (!assetHandle.IsValid())
                {
                    continue;
                }

                if (assetHandle.Result == null)
                {
                    continue;
                }

                if (obj is Sprite sprite)
                {
                    if (assetHandle.Result is Texture2D texture && sprite.texture == texture)
                    {
                        key = pair.Key;
                        break;
                    }
                }
                else
                {
                    if (assetHandle.Result.Equals(obj))
                    {
                        key = pair.Key;
                        break;
                    }
                }
            }

            Instance.ReleaseLoadedAsseInternal(key, poolName, targetAssetCategory);
        }

        private static AssetCategory GetTargetAssetCategory(AssetCategory assetCategory)
        {
            var targetAssetCategory = GetCurrentTargetAssetCategory(assetCategory);
            if (targetAssetCategory == AssetCategory.None)
            {
                targetAssetCategory = GetBeforeTargetAssetCategory(assetCategory);
            }

            return targetAssetCategory;
        }

        public static void ReleaseLoadedAsset(string key, string poolName = DefaultPoolName, AssetCategory assetCategory = AssetCategory.None)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            var targetAssetCategory = GetTargetAssetCategory(assetCategory);
            if (Instance.AssetPoolHubDict[targetAssetCategory].ContainsPool(poolName) == false)
            {
                return;
            }

            Instance.ReleaseLoadedAsseInternal(key, poolName, targetAssetCategory);
        }

        private void ReleaseLoadedAsseInternal(string key, string poolName, AssetCategory assetCategory)
        {
            var assetPool = AssetPoolHubDict[assetCategory].GetPool(poolName);
            assetPool.RemoveLoadAsset(key);
        }
    }
}