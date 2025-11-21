using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Manager
{
    public partial class AssetManager
    {
        [Serializable]
        public abstract class BasePoolHub<TPool> : IDisposable where TPool : BasePool, IDisposable, new()
        {
            [ShowInInspector] private readonly SerializableDictionary<string, TPool> _poolDict = new();
            public IReadOnlyDictionary<string, TPool> PoolDict => _poolDict;

            public void AddPool(string poolName)
            {
                if (_poolDict.ContainsKey(poolName))
                {
                    Debug.LogWarning($"Already contains pool. [{poolName}]");
                    return;
                }

                var pool = new TPool();
                pool.Initialize(poolName);
                _poolDict[poolName] = pool;
            }

            public TPool GetPool(string poolName)
            {
                if (!_poolDict.ContainsKey(poolName))
                {
                    Debug.LogWarning($"Not contains pool. [{poolName}]");
                    return null;
                }

                return _poolDict[poolName];
            }

            public void RemovePool(string poolName)
            {
                if (!_poolDict.ContainsKey(poolName))
                {
                    Debug.LogWarning($"Not contains pool. [{poolName}]");
                    return;
                }
                 
                _poolDict.TryGetValue(poolName, out var pool);
                if (pool != null)
                {
                    pool.Dispose();
                }
                _poolDict.Remove(poolName);
            }

            public bool ContainsPool(string poolName)
            {
                return _poolDict.ContainsKey(poolName);
            }

            public void Dispose()
            {
                var tempPoolDict = new Dictionary<string, TPool>();
                foreach (KeyValuePair<string, TPool> pair in _poolDict)
                {
                    if (!ShouldDispose(pair.Key))
                    {
                        tempPoolDict.Add(pair.Key, pair.Value);
                        continue;
                    }

                    pair.Value.Dispose();
                }

                _poolDict.Clear();
                foreach (var pair in tempPoolDict)
                {
                    _poolDict.Add(pair.Key, pair.Value);
                }
            }

            protected virtual bool ShouldDispose(string poolName)
            {
                return true;
            }
        }

        public abstract class BasePool : IDisposable
        {
            public abstract void Initialize(string poolName);
            public abstract void Dispose();
        }
    }
}