using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace Manager
{
    public partial class AssetManager
    {
        // ObjectPool
        public GameObject GetFromSource(GameObject origin, string poolName = DefaultPoolName, AssetCategory assetCategory = AssetCategory.None)
        {
            if (origin == null)
            {
                return null;
            }

            var targetAssetCategory = GetCurrentTargetAssetCategory(assetCategory);

            var targetPoolName = poolName;

            origin.TryGetComponent(out UISlot slot);
            if (slot)
            {
                slot.SetExtraName(poolName);
                targetPoolName = slot.SlotName;
            }

            if (!ObjectPoolHubDict[targetAssetCategory].ContainsPool(targetPoolName))
            {
                ObjectPoolHubDict[targetAssetCategory].AddPool(targetPoolName);
            }

            return ObjectPoolHubDict[targetAssetCategory].PoolDict[targetPoolName].GetFromSource(origin);
        }

        public void Return(GameObject moduleGameObject, string poolName = DefaultPoolName, AssetCategory assetCategory = AssetCategory.None)
        {
            if (moduleGameObject == null)
            {
                return;
            }

            var targetAssetCategory = GetCurrentTargetAssetCategory(assetCategory);
            if (targetAssetCategory == AssetCategory.None)
            {
                targetAssetCategory = GetBeforeTargetAssetCategory(assetCategory);
            }

            var targetPoolName = poolName;
            moduleGameObject.TryGetComponent(out UISlot slot);
            if (slot)
            {
                slot.SetExtraName(poolName);
                targetPoolName = slot.SlotName;
            }

            if (!ObjectPoolHubDict[targetAssetCategory].ContainsPool(targetPoolName))
            {
                ObjectPoolHubDict[targetAssetCategory].AddPool(targetPoolName);
            }

            ObjectPoolHubDict[targetAssetCategory].PoolDict[targetPoolName].Return(moduleGameObject);
        }

        public class ObjectPoolHub : BasePoolHub<ObjectPool>
        {
            public void Cleanup()
            {
                foreach (var pool in PoolDict.Values)
                {
                    pool.ForceCleanupDeactivated();
                }
            }

            public void CleanupPool(string poolName)
            {
                if (PoolDict.TryGetValue(poolName, out var pool))
                {
                    pool.ForceCleanupDeactivated();
                }
            }
        }

        public class InstanceManagerManaged : MonoBehaviour { }

        public class ObjectPool : BasePool
        {
            private readonly Dictionary<GameObject, List<GameObject>> _objectLists = new();
            private readonly HashSet<GameObject> _activatedObjects = new();
            private GameObject _deactivatedObjectContainer;

            private string _poolName;

            public override void Initialize(string poolName)
            {
                _poolName = poolName;

                string deactivatedObjectsName = $"Deactivated Objects - {poolName}";
                _deactivatedObjectContainer = new GameObject(deactivatedObjectsName);
                // deactivatedObjectContainer.transform.parent = transform;
                _deactivatedObjectContainer.SetActive(false);

                SceneManager.sceneUnloaded += OnSceneUnloaded;
            }

            public override void Dispose()
            {
                Cleanup();
                if (_deactivatedObjectContainer != null)
                {
                    UnmanagedDestroy(_deactivatedObjectContainer);
                }
            }

            private void OnSceneUnloaded(Scene arg0)
            {
                _activatedObjects.RemoveWhere(x =>
                {
                    if (x != null && x.scene == arg0)
                    {
                        x.transform.SetParent(_deactivatedObjectContainer.transform, false);
#if UNITY_EDITOR
                        Debug.LogWarningFormat($"InstanceManager: [{_poolName}] Object is not returned: {0}", x.name);
#endif
                        return true;
                    }

                    return false;
                });
            }

            /// <summary>
            /// sourceObject의 관리되는 복제 객체를 가져옵니다.
            /// </summary>
            public GameObject GetFromSource(GameObject sourceObject)
            {
                if (sourceObject == null)
                    return null;

                List<GameObject> objectList = GetObjectList(sourceObject);
                var gameObject = GetActivatableObjectFromList(objectList);

                if (gameObject == null)
                {
                    objectList.Add(gameObject = InstantiateForPool(sourceObject));
                }
                //else
                //{
                //    gameObject.GetOrAddComponent<InstanceManagerManaged>();
                //}

                return ActivateObject(gameObject);
            }

            public GameObject GetFromSource(GameObject sourceObject, Transform parent, bool worldPositionStays = false)
            {
                var gameObject = GetFromSource(sourceObject);
                if (gameObject != null)
                {
                    gameObject.transform.SetParent(parent, worldPositionStays);
                }

                return gameObject;
            }

            private List<GameObject> GetObjectList(GameObject sourceObject)
            {
                List<GameObject> objectList;

                if (_objectLists.TryGetValue(sourceObject, out objectList))
                    return objectList;

                return (_objectLists[sourceObject] = new List<GameObject>());
            }

            private GameObject GetActivatableObjectFromList(List<GameObject> gameObjectList)
            {
                for (int i = gameObjectList.Count - 1; i >= 0; --i)
                {
                    if (gameObjectList[i] == null)
                    {
                        gameObjectList.RemoveAt(i);
                    }
                    else if (!_activatedObjects.Contains(gameObjectList[i]))
                    {
                        return gameObjectList[i];
                    }
                }

                return null;
            }

            private GameObject InstantiateForPool(GameObject source)
            {
                var gameObject = UnmanagedInstantiate(source);

                gameObject.GetOrAddComponent<InstanceManagerManaged>();

                return gameObject;
            }

            private GameObject ActivateObject(GameObject gameObject)
            {
                if (gameObject != null)
                {
                    gameObject.transform.SetParent(null, false);
                    _activatedObjects.Add(gameObject);
                }

                return gameObject;
            }

            /// <summary>
            /// 오브젝트를 비활성화 시키고, ObjectPool에 돌려 놓습니다.
            /// </summary>
            public void Return(GameObject gameObject)
            {
                if (gameObject == null)
                {
#if UNITY_EDITOR
                    Debug.LogWarning("InstanceManager: Return failed (gameObject is null or destroyed)");
#endif
                    return;
                }

                using (ListPool<InstanceManagerManaged>.Get(out var children))
                {
                    gameObject.GetComponentsInChildren(true, children);

                    for (int i = children.Count - 1; i >= 0; --i)
                    {
                        ReturnInternal(children[i].gameObject);
                    }
#if UNITY_EDITOR
                    if (children.Count == 0)
                        Debug.LogWarningFormat("InstanceManager: Return failed (InstanceManagerManaged components not found)");
#endif
                }
            }

            private void ReturnInternal(GameObject gameObject)
            {
                if (_activatedObjects.Remove(gameObject))
                {
                    gameObject.transform.SetParent(_deactivatedObjectContainer.transform, false); // false가 올바른 값입니다. 2A에 해당 값으로 반영해주세요.
                }
#if UNITY_EDITOR
                else if (gameObject.transform.parent == _deactivatedObjectContainer.transform)
                {
                    Debug.LogWarningFormat("InstanceManager: Return failed (Already returned object '{0}')", gameObject);
                }
                else
                {
                    Debug.LogWarningFormat("InstanceManager: Return failed (Non-managed instance '{0}')", gameObject);
                    UnmanagedDestroy(gameObject);
                }
#endif
            }

            /// <summary>
            /// 비 활성화 된 오브젝트들을 정리합니다.
            /// </summary>
            public void ForceCleanupDeactivated()
            {
                foreach (var pair in _objectLists.GCFreeIterator())
                {
                    var list = pair.Value;

                    for (int i = list.Count - 1; i >= 0; --i)
                    {
                        if (!_activatedObjects.Contains(list[i]))
                        {
                            UnmanagedDestroy(list[i]);
                            list.RemoveAt(i);
                        }
                    }
                }
            }

            private void Cleanup()
            {
                foreach (var pair in _objectLists.GCFreeIterator())
                {
                    var list = pair.Value;
                    foreach (var obj in list)
                    {
                        UnmanagedDestroy(obj);
                    }

                    list.Clear();
                }

                _objectLists.Clear();
            }
        }
    }
}