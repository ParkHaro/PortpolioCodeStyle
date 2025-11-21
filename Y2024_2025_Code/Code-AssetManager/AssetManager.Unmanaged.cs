using UnityEngine;

namespace Manager
{
    public partial class AssetManager
    {
        public static T UnmanagedInstantiate<T>(T source, Transform parent = null, bool worldPositionStays = false)
            where T : Object
        {
            return Instance.UnmanagedInstantiateInternal(source, parent, worldPositionStays);
        }

        private T UnmanagedInstantiateInternal<T>(T original, Transform parent, bool worldPositionStays)
            where T : Object
        {
            return (T)Object.Instantiate((Object)original, parent, worldPositionStays);
        }

        public static void UnmanagedDestroy(GameObject source)
        {
            Instance.UnmanagedDestroyInternal(source);
        }

        private void UnmanagedDestroyInternal(GameObject source)
        {
            Destroy(source);
        }
        
        public static void UnmanagedDestroyImmediate(GameObject source)
        {
            Instance.UnmanagedDestroyImmediateInternal(source);
        }
        
        private void UnmanagedDestroyImmediateInternal(GameObject source)
        {
            DestroyImmediate(source);
        }

        public static T ResourcesLoad<T>(string path)
            where T : Object
        {
            return Instance.ResourcesLoadInternal<T>(path);
        }

        private T ResourcesLoadInternal<T>(string path)
            where T : Object
        {
            var loadObject = Resources.Load<T>(path);
            if (loadObject == null) DebugHelper.LogError($"로드에 실패하였습니다. => {path}");

            return loadObject;
        }
    }
}