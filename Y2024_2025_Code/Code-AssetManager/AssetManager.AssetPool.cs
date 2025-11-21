using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Manager
{
    public partial class AssetManager
    {
        private void ReleaseAssets(HashSet<string> poolNameSet)
        {
            foreach (var assetPoolHub in _assetPoolHubDict.Values)
            {
                assetPoolHub.ReleaseAssets(poolNameSet);
            }
        }

        [Serializable]
        public class AssetPoolHub : BasePoolHub<AssetPool>
        {
            public void AddLoadAsset(string poolName, string key, AsyncOperationHandle handle)
            {
                PoolDict[poolName].AddLoadAsset(key, handle);
            }

            protected override bool ShouldDispose(string poolName)
            {
                return !Instance.IsIgnorePool(poolName);
            }

            public void ReleaseAssets(HashSet<string> poolNameSet)
            {
                foreach (var poolName in poolNameSet)
                {
                    if (!ContainsPool(poolName))
                    {
                        continue;
                    }

                    var pool = GetPool(poolName);
                    pool.Dispose();
                    RemovePool(poolName);
                }
            }

            public void MovePoolAssets(string fromPoolName, string toPoolName)
            {
                if (!ContainsPool(fromPoolName))
                {
                    return;
                }

                if (!ContainsPool(toPoolName))
                {
                    AddPool(toPoolName);
                }

                var fromPool = GetPool(fromPoolName);
                var toPool = GetPool(toPoolName);

                fromPool.MovePool(toPool);
                DebugHelper.Log($"Move Pool Assets {fromPoolName} -> {toPoolName}");
            }
        }

        [Serializable]
        public class AssetPool : BasePool
        {
            [ShowInInspector] private readonly SerializableDictionary<string, List<GameObject>> _instantiatedAssetObjectDict = new();
            public IReadOnlyDictionary<string, List<GameObject>> InstantiatedAssetObjectDict => _instantiatedAssetObjectDict;
            private readonly Dictionary<GameObject, string> _instantiatedAssetObjectKeyDict = new();
            public IReadOnlyDictionary<GameObject, string> InstantiatedAssetObjectKeyDict => _instantiatedAssetObjectKeyDict;

            // Load Asset
            private readonly Dictionary<string, LoadedAsset> _loadedAssetHandleDict = new();
            public IReadOnlyDictionary<string, LoadedAsset> LoadedAssetHandleDict => _loadedAssetHandleDict;

            private string _poolName;
            public string PoolName => _poolName;

            public class LoadedAsset
            {
                public int Count;
                public AsyncOperationHandle Handle;
            }

            public void AddInstantiatedAssetObject(string key, GameObject gameObject)
            {
                if (_instantiatedAssetObjectDict.TryGetValue(key, out var list))
                {
                    list.Add(gameObject);
                    _instantiatedAssetObjectDict[key] = list;
                }
                else
                {
                    _instantiatedAssetObjectDict[key] = new List<GameObject>
                    {
                        gameObject
                    };
                }

                _instantiatedAssetObjectKeyDict[gameObject] = key;
            }

            public void RemoveInstantiatedAssetObject(GameObject gameObject)
            {
                if (_instantiatedAssetObjectKeyDict.TryGetValue(gameObject, out var key))
                {
                    if (_instantiatedAssetObjectDict.TryGetValue(key, out var list))
                    {
                        list.Remove(gameObject);
                        if (list.Count == 0)
                        {
                            _instantiatedAssetObjectDict.Remove(key);
                        }
                        else
                        {
                            _instantiatedAssetObjectDict[key] = list;
                        }
                    }

                    _instantiatedAssetObjectKeyDict.Remove(gameObject);
                }
            }

            public AsyncOperationHandle GetAssetHandleWithAddCount(string key)
            {
                if (_loadedAssetHandleDict.TryGetValue(key, out var loadedAsset))
                {
                    loadedAsset.Count++;
                    return loadedAsset.Handle;
                }

                return default;
            }

            public void AddLoadAsset(string key, AsyncOperationHandle handle)
            {
                //if (key.Contains(".ogg"))
                //    DebugHelper.Log($"[Test] {key} Load Load Asset", Color.green);

                if (_loadedAssetHandleDict.TryGetValue(key, out var loadedAsset))
                {
                    loadedAsset.Count++;
                }
                else
                {
                    _loadedAssetHandleDict[key] = new LoadedAsset
                    {
                        Count = 1,
                        Handle = handle
                    };
                }
            }

            public void RemoveLoadAsset(string key)
            {
                if (string.IsNullOrEmpty(key))
                {
                    return;
                }

                if (_loadedAssetHandleDict.TryGetValue(key, out var loadedAsset))
                {
                    //if (key.Contains(".ogg"))
                    //    DebugHelper.Log($"[Test] {key} Remove Load Asset", Color.red);

                    loadedAsset.Count--;
                    if (loadedAsset.Count > 0)
                    {
                        return;
                    }

                    _loadedAssetHandleDict.Remove(key);
                    if (loadedAsset.Handle.IsValid() && loadedAsset.Handle.Result != null)
                    {
                        // Debug.LogWarning($"Release : [{key}]");
                        Addressables.Release(loadedAsset.Handle);
                    }
                }
            }

            public override void Initialize(string poolName)
            {
                _poolName = poolName;
            }

            public override void Dispose()
            {
                foreach (var Pair in _instantiatedAssetObjectDict)
                {
                    foreach (var gameObject in Pair.Value)
                    {
                        if (gameObject != null)
                        {
                            UnmanagedDestroy(gameObject);
                        }
                    }

                    Pair.Value.Clear();
                }

                _instantiatedAssetObjectDict.Clear();
                _instantiatedAssetObjectKeyDict.Clear();

                DisposeLoadedAsset();
                ClearLoadedAsset();
            }

            public void MovePool(AssetPool toPool)
            {
                foreach (var pair in _loadedAssetHandleDict.ToList())
                {
                    MoveLoadAsset(toPool, pair);
                }
            }

            private void DisposeLoadedAsset()
            {
                foreach (var Pair in _loadedAssetHandleDict)
                {
                    // Debug.LogWarning($"Dispose : AssetPool Loaded [{_poolName}] / Key [{Pair.Key}]");
                    var loadedAsset = Pair.Value;
                    if (loadedAsset.Handle.IsValid() && loadedAsset.Handle.Result != null)
                    {
                        // Debug.LogWarning($"Result : [{loadedAsset.Handle.Result}]");
                        Addressables.Release(loadedAsset.Handle);
                    }
                }
            }

            private void ClearLoadedAsset()
            {
                _loadedAssetHandleDict.Clear();
            }

            private void MoveLoadAsset(AssetPool toPool, KeyValuePair<string, LoadedAsset> pair)
            {
                toPool._loadedAssetHandleDict.Add(pair.Key, pair.Value);
                _loadedAssetHandleDict.Remove(pair.Key);
            }

            public void CleanInvalidInstances()
            {
                var removedCountPerKey = new Dictionary<string, int>();
                var goKeysToRemove = new List<GameObject>();

                foreach (var kv in _instantiatedAssetObjectKeyDict)
                {
                    var go = kv.Key;
                    var key = kv.Value;

                    if (go == null)
                    {
                        // 누락된 GameObject → key 기준으로 Count 정리 준비
                        if (!removedCountPerKey.ContainsKey(key))
                            removedCountPerKey[key] = 0;

                        removedCountPerKey[key]++;
                        goKeysToRemove.Add(go);
                    }
                }

                // 역방향 딕셔너리에서 제거
                foreach (var go in goKeysToRemove)
                {
                    _instantiatedAssetObjectKeyDict.Remove(go);
                }

                // 메인 Dictionary에서 null 제거
                var keysToRemove = new List<string>();

                foreach (var pair in _instantiatedAssetObjectDict)
                {
                    var key = pair.Key;
                    var goList = pair.Value;

                    goList.RemoveAll(go => go == null);

                    if (goList.Count == 0)
                        keysToRemove.Add(key);
                }

                foreach (var key in keysToRemove)
                {
                    _instantiatedAssetObjectDict.Remove(key);
                }

                // 로드된 에셋 카운트 차감
                foreach (var pair in removedCountPerKey)
                {
                    for (int i = 0; i < pair.Value; i++)
                    {
                        RemoveLoadAsset(pair.Key);
                    }
                }
            }
        }

        public AssetPoolHub CurrentAssetPoolHub => AssetPoolHubDict[CurrentAssetCategory];
        public ObjectPoolHub CurrentObjectPoolHub => ObjectPoolHubDict[CurrentAssetCategory];
    }
}