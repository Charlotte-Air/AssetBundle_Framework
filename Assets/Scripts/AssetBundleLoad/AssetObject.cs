using System;
using UnityEngine;

namespace AssetBundleTool
{
    
    public class AssetObject : MonoBehaviour
    {

        [SerializeField, HideInInspector]
        public string BundlePath;   // { get; private set; }
        [SerializeField, HideInInspector]
        public string AssetName;    // { get; private set; }

        private IAssetReferenceHandler mRefHandler;

        private bool mStartRef;
        

        /// <summary>
        /// 异步加载，返回一个带脚本管理的asset (prefab实例化后的gameobject)
        /// </summary>
        /// <param name="bundleName">ab包路径(默认asset名字跟ab包名一样)</param>
        /// <param name="callback">返回带脚本管理的asset</param>
        /// <returns>同步返回加载器，可以观察进度和取消加载</returns>
        public static AssetLoader LoadAsync(string bundleName, Action<AssetObject> callback)
        {
            return LoadAsync(bundleName, null, callback);
        }

        /// <summary>
        /// 异步加载，返回一个带脚本管理的asset (prefab实例化后的gameobject)
        /// </summary>
        /// <param name="bundleName">ab包路径</param>
        /// <param name="assetName">包内的资源名称</param>
        /// <param name="callback">返回带脚本管理的asset</param>
        /// <returns>同步返回加载器，可以观察进度和取消加载</returns>
        public static AssetBundleTool.AssetLoader LoadAsync(string bundleName, string assetName, Action<AssetObject> callback)
        {
            return AssetManager.LoadAsync(bundleName, assetName, (loader) =>
            {
                if (callback != null)
                {
                    var obj = CreateAssetObject(loader);
                    callback(obj);
                }
            });
        }

        /// <summary>
        /// 异步加载，返回一个带脚本管理的asset (prefab实例化后的gameobject)
        /// </summary>
        /// <param name="modName">此ab包对应的mod名，null表示使用当前mod，""表示忽略mod名</param>
        /// <param name="bundleName">ab包路径</param>
        /// <param name="assetName">包内的资源名称</param>
        /// <param name="callback">返回带脚本管理的asset</param>
        /// <returns>同步返回加载器，可以观察进度和取消加载</returns>
        public static AssetLoader LoadAsync(string modName, string bundleName, string assetName, Action<AssetObject> callback)
        {
            return AssetManager.LoadAsync(modName, bundleName, assetName, (loader) =>
            {
                if (callback != null)
                {
                    var obj = CreateAssetObject(loader);
                    callback(obj);
                }
            });
        }

        /// <summary>
        /// 异步加载，返回一个带脚本管理的asset (prefab实例化后的gameobject)
        /// </summary>
        /// <param name="path">带modname的完整路径</param>
        /// <param name="callback">返回带脚本管理的asset</param>
        /// <returns>同步返回加载器，可以观察进度和取消加载</returns>
        public static AssetLoader LoadAsyncFullPath(string path, Action<AssetObject> callback)
        {
            return AssetManager.LoadAsyncFullPath(path, (loader) =>
            {
                if (callback != null)
                {
                    var obj = CreateAssetObject(loader);
                    callback(obj);
                }
            });
        }

        /// <summary>
        /// 异步加载，支持加载以文件夹打ab包的资源，直接传一个完整路径，然后自动解析出bundleName和assetName。
        /// 比如：UI/wnd/Panel_Control.prefab 会解析出 bundleName：UI/wnd.assetbundles, assetName: Panel_Control.prefab
        /// 返回一个带脚本管理的asset (prefab实例化后的gameobject)
        /// </summary>
        /// <param name="modeName">此ab包对应的mod名，null表示使用当前mod，""表示忽略mod名</param>
        /// <param name="assetPath">资源的完整路径</param>
        /// <param name="callback">返回带脚本管理的asset</param>
        /// <returns>同步返回加载器，可以观察进度和取消加载</returns>
        public static AssetLoader LoadAsyncPackByDir(string modeName, string assetPath, Action<AssetObject> callback)
        {
            assetPath = assetPath.Replace('\\', '/');
            int splitIndex = assetPath.LastIndexOf('/');
            string bundleName = assetPath.Substring(0, splitIndex) + ".assetbundles";
            string assetName = assetPath.Substring(splitIndex + 1);
            return LoadAsync(modeName, bundleName, assetName, callback);
        }

        /// <summary>
        /// 同步加载，返回一个带脚本管理的asset (prefab实例化后的gameobject)
        /// </summary>
        /// <param name="bundleName">ab包路径(默认asset名字跟ab包名一样)</param>
        /// <returns>返回带脚本管理的asset</returns>
        public static AssetObject Load(string bundleName)
        {
            return Load(bundleName, null);
        }

        /// <summary>
        /// 同步加载，返回一个带脚本管理的asset (prefab实例化后的gameobject)
        /// </summary>
        /// <param name="bundleName">ab包路径</param>
        /// <param name="assetName">包内的资源名称</param>
        /// <returns>返回带脚本管理的asset</returns>
        public static AssetObject Load(string bundleName, string assetName)
        {
            var loader = AssetManager.Load(bundleName, assetName);
            return CreateAssetObject(loader);
        }

        /// <summary>
        /// 同步加载，返回一个带脚本管理的asset (prefab实例化后的gameobject)
        /// </summary>
        /// <param name="modName">此ab包对应的mod名，null表示使用当前mod，""表示忽略mod名</param>
        /// <param name="bundleName">ab包路径</param>
        /// <param name="assetName">包内的资源名称</param>
        /// <returns>返回带脚本管理的asset</returns>
        public static AssetObject Load(string modName, string bundleName, string assetName)
        {
            var loader = AssetManager.Load(modName, bundleName, assetName);
            return CreateAssetObject(loader);
        }

        /// <summary>
        /// 同步加载，返回一个带脚本管理的asset (prefab实例化后的gameobject)
        /// </summary>
        /// <param name="path">带modname的完整路径</param>
        /// <returns>返回带脚本管理的asset</returns>
        public static AssetObject LoadFullPath(string path)
        {
            var loader = AssetManager.LoadFullPath(path);
            return CreateAssetObject(loader);
        }


        private static AssetObject CreateAssetObject(AssetLoader loader)
        {
            //todo 如果要做一层gameobject缓存，可以加在这里

            if (loader.AssetObject != null && loader.AssetObject is GameObject)
            {
                var gameObject = (GameObject)Instantiate(loader.AssetObject);
                AssetObject assetObj = gameObject.AddComponent<AssetObject>();
                assetObj.Init(loader.BundlePath, loader.AssetName);
                return assetObj;
            }
            return null;
        }

        public void Init(string bundlePath, string assetName)
        {
            this.BundlePath = bundlePath;
            this.AssetName = assetName;
            this.mRefHandler = AssetManager.Instance;
            //首次初始化awake不会进去，这里要调用一次
            if (mRefHandler != null)
            {
                mStartRef = mRefHandler.AddAssetRef(BundlePath);
            }
        }

        void Awake()
        {
            this.mRefHandler = AssetManager.Instance;
            //上层clone的时候这里要自动加引用计数
            if (mRefHandler != null)
            {
                mStartRef = mRefHandler.AddAssetRef(BundlePath);
            }
        }

        void OnDestroy()
        {
            if (mRefHandler != null && mStartRef)
            {
                mRefHandler.RemoveAssetRef(BundlePath);
            }
        }

    }

}
