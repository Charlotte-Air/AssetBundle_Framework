using System;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleTool
{
    
    public class AssetManager : MonoBehaviour, IAssetReferenceHandler
    {
        public static AssetManager Instance { get; private set; }

        public static string LOCAL_PATH { get; private set; }
        public static string EXPAND_PATH { get; private set; }
        public static string MOD_NAME { get; private set; }


        private Dictionary<string, AssetBundleManifest> mManifest = new Dictionary<string, AssetBundleManifest>();

        private Dictionary<string, AssetBundle> mBundleCache = new Dictionary<string, AssetBundle>();
        private Dictionary<string, AssetLoader> mCurLoadingBundle = new Dictionary<string, AssetLoader>();
        private Dictionary<string, List<string>> mDepsListCache = new Dictionary<string, List<string>>();
        private Queue<AssetLoader> mDelayTaskQueue = new Queue<AssetLoader>();

        //所有从Bundle实例化对象的引用计数
        private readonly Dictionary<string, int> mBundleRef = new Dictionary<string, int>();

        private IAssetDownloadHandler mDownloadHandler;
        private IAssetLocalFileHandler mLocalFileHandler;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {

        }

        public void InitRoot(string localPath, string expandPath)
        {
            LOCAL_PATH = localPath;
            EXPAND_PATH = expandPath;
        }

        public void InitMod(string modName)
        {
            MOD_NAME = modName;
            LoadShaderVariants();
            LoadManiFest(MOD_NAME);
        }


        /// <summary>
        /// 异步加载，返回一个加载器(加载非prefab的asset专用)
        /// </summary>
        /// <param name="bundleName">ab包路径(默认asset名字跟ab包名一样)</param>
        /// <param name="callback">加载器</param>
        /// <returns>同步返回加载器，可以观察进度和取消加载</returns>
        public static AssetLoader LoadAsync(string bundleName, Action<AssetLoader> callback)
        {
            return LoadAsync(null, bundleName, null, callback);
        }

        /// <summary>
        /// 异步加载，返回一个加载器(加载非prefab的asset专用)
        /// </summary>
        /// <param name="bundleName">ab包路径</param>
        /// <param name="assetName">包内的资源名称</param>
        /// <param name="callback">加载器</param>
        /// <returns>同步返回加载器，可以观察进度和取消加载</returns>
        public static AssetLoader LoadAsync(string bundleName, string assetName, Action<AssetLoader> callback)
        {
            return LoadAsync(null, bundleName, assetName, callback);
        }

        /// <summary>
        /// 异步加载，返回一个加载器(加载非prefab的asset专用)
        /// </summary>
        /// <param name="modName">此ab包对应的mod名，null表示使用当前mod，""表示忽略mod名</param>
        /// <param name="bundleName">ab包路径</param>
        /// <param name="assetName">包内的资源名称</param>
        /// <param name="callback">加载器</param>
        /// <returns>同步返回加载器，可以观察进度和取消加载</returns>
        public static AssetLoader LoadAsync(string modName, string bundleName, string assetName, Action<AssetLoader> callback)
        {
            if (modName == null) //如果modeName为空，默认使用当前mod
            {
                modName = MOD_NAME;
            }
            var loader = new AssetLoader(modName, bundleName, assetName, true, callback);
            loader.SetLocalFileHandler(Instance.mLocalFileHandler);
            loader.SetDownloadHandler(Instance.mDownloadHandler);
            Instance.LoadAssetBundle(loader);
            return loader;
        }

        /// <summary>
        /// 异步加载，返回一个加载器(加载非prefab的asset专用)
        /// </summary>
        /// <param name="path">带modname的完整路径</param>
        /// <param name="callback">加载器</param>
        /// <returns>同步返回加载器，可以观察进度和取消加载</returns>
        public static AssetLoader LoadAsyncFullPath(string path, Action<AssetLoader> callback)
        {
            var loader = new AssetLoader(string.Empty, path, null, true, callback);
            Instance.LoadAssetBundle(loader);
            return loader;
        }

        /// <summary>
        /// 同步加载，返回一个加载器(加载非prefab的asset专用)
        /// </summary>
        /// <param name="bundleName">ab包路径(默认asset名字跟ab包名一样)</param>
        /// <returns>加载器</returns>
        public static AssetLoader Load(string bundleName)
        {
            return Load(null, bundleName, null);
        }

        /// <summary>
        /// 同步加载，返回一个加载器(加载非prefab的asset专用)
        /// </summary>
        /// <param name="bundleName">ab包路径</param>
        /// <param name="assetName">包内的资源名称</param>
        /// <returns>加载器</returns>
        public static AssetLoader Load(string bundleName, string assetName)
        {
            return Load(null, bundleName, assetName);
        }

        /// <summary>
        /// 同步加载，返回一个加载器(加载非prefab的asset专用)
        /// </summary>
        /// <param name="modName">此ab包对应的mod名，null表示使用当前mod，""表示忽略mod名</param>
        /// <param name="bundleName">ab包路径</param>
        /// <param name="assetName">包内的资源名称</param>
        /// <returns>加载器</returns>
        public static AssetLoader Load(string modName, string bundleName, string assetName)
        {
            if (modName == null) //如果modeName为空，默认使用当前mod
            {
                modName = MOD_NAME;
            }
            var loader = new AssetLoader(modName, bundleName, assetName, false, null);
            loader.SetLocalFileHandler(Instance.mLocalFileHandler);
            loader.SetDownloadHandler(Instance.mDownloadHandler);
            Instance.LoadAssetBundle(loader);
            return loader;
        }

        /// <summary>
        /// 同步加载，返回一个加载器(加载非prefab的asset专用)
        /// </summary>
        /// <param name="path">带modname的完整路径</param>
        /// <returns>加载器</returns>
        public static AssetLoader LoadFullPath(string path)
        {
            var loader = new AssetLoader(string.Empty, path, null, false, null);
            Instance.LoadAssetBundle(loader);
            return loader;
        }

        /// <summary>
        /// 加载ManiFest
        /// </summary>
        private bool LoadManiFest(string modName)
        {
            if (!mManifest.ContainsKey(modName))
            {
                string bundleName = modName;
                int split = bundleName.LastIndexOf('/');
                if (split != -1)
                    bundleName = bundleName.Substring(split + 1);
                var loader = AssetManager.Load(modName, bundleName, "AssetBundleManifest");
                if (loader.IsSuccess)
                {
                    var manifest = loader.AssetObject as AssetBundleManifest;
                    mManifest.Add(modName, manifest);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 加载ShaderVariants
        /// </summary>
        private void LoadShaderVariants()
        {
            var loader = AssetManager.Load(string.Empty, "shadervariants.assetbundles", "ShaderVariants");
            ShaderVariantCollection svc = loader.AssetObject as ShaderVariantCollection;
            if (svc == null)
            {
                Debug.LogError("ShaderVariantCollection can not be Found ");
                return;
            }
            svc.WarmUp();

            //ShaderVariants从缓存里移除，一直存在不会卸载
            mBundleCache.Remove(loader.BundlePath);
        }

        private void LoadAssetBundle(AssetLoader loader, bool isAsyncDelay = true)
        {
            if (loader.ModName != string.Empty)   //加载的是非根目录资源，需要manifest
            {
                if (!mManifest.TryGetValue(loader.ModName, out var manifest))  //manifest未加载
                {
                    if (loader.AssetName != "AssetBundleManifest")  //加载的不是manifest本身
                    {
                        //尝试加载其他mod的manifest
                        var manifestLoadSuc = LoadManiFest(loader.ModName);
                        if (!manifestLoadSuc) //加载失败
                        {
                            //标记为失败，下次直接返回结果
                            mManifest.Add(loader.ModName, null);

                            Debug.LogError(loader.ModName + " AssetBundleManifest not found!!!");
                            loader.OnBundleLoadCallBack(null);
                            return;
                        }
                    }
                }
                else if (manifest == null)  //有manifest但是加载不出，直接返回错误
                {
                    Debug.LogError(loader.ModName + " AssetBundleManifest not found!!!");
                    loader.OnBundleLoadCallBack(null);
                    return;
                }
            }

            if (string.IsNullOrEmpty(loader.BundleName))    //BundleName为空，返回失败
            {
                if (loader.IsAsync && isAsyncDelay)
                    mDelayTaskQueue.Enqueue(loader);
                else
                    loader.OnBundleLoadCallBack(null);
                return;
            }

            if (mBundleCache.TryGetValue(loader.BundlePath, out var bundleCache))    //如果缓存里有，直接返回
            {
                if (loader.IsAsync && isAsyncDelay)
                    mDelayTaskQueue.Enqueue(loader);
                else
                    loader.OnBundleLoadCallBack(bundleCache);
            }
            else
            {
                if (loader.IsAsync)
                {
                    if (mCurLoadingBundle.TryGetValue(loader.BundlePath, out var loadingLoader))    //如果其他loader也在加载同一资源，添加到同一个处理进程
                    {
                        loadingLoader.AddLinkedLoader(loader);
                    }
                    else
                    {
                        //添加到加载任务中
                        mCurLoadingBundle.Add(loader.BundlePath, loader);
                        //开始加载
                        loader.StartLoading(GetDepList(loader.ModName, loader.BundleName), (bundle) =>
                        {
                            //添加进bundle缓存
                            mBundleCache.Add(loader.BundlePath, bundle);
                            //移除加载任务
                            mCurLoadingBundle.Remove(loader.BundlePath);
                        });
                    }
                }
                else
                {
                    loader.StartLoading(GetDepList(loader.ModName, loader.BundleName));
                    //添加进bundle缓存
                    mBundleCache.Add(loader.BundlePath, loader.Bundle);
                }
            }
        }

        /// <summary>
        /// 获取依赖列表
        /// </summary>
        /// <param name="modName"></param>
        /// <param name="bundleName"></param>
        /// <returns></returns>
        private List<string> GetDepList(string modName, string bundleName)
        {
            if (mManifest.TryGetValue(modName, out var manifest) && manifest != null)
            {
                var bundlePath = AssetLoader.GetBundlePath(modName, bundleName);
                if (!mDepsListCache.TryGetValue(bundlePath, out var depList))
                {
                    depList = new List<string>();
                    var deps = manifest.GetAllDependencies(bundleName);
                    for (int i = 0; i < deps.Length; ++i)
                    {
                        var dep = deps[i];
                        if (dep != "shadervariants.assetbundles")
                        {
                            depList.Add(dep);
                        }
                    }
                    mDepsListCache.Add(bundlePath, depList);
                }
                return depList;
            }
            return null;
        }

        bool IAssetReferenceHandler.AddAssetRef(string bundlePath)
        {
            if (string.IsNullOrEmpty(bundlePath) || !mBundleCache.ContainsKey(bundlePath))
                return false;

            if (mBundleRef.TryGetValue(bundlePath, out var refCount))
            {
                mBundleRef[bundlePath] = refCount + 1;
            }
            else
            {
                mBundleRef[bundlePath] = 1;
            }
            return true;
        }

        bool IAssetReferenceHandler.RemoveAssetRef(string bundlePath)
        {
            if (mBundleRef.TryGetValue(bundlePath, out var refCount))
            {
                var newCount = refCount - 1;
                mBundleRef[bundlePath] = newCount;
                if (newCount <= 0)
                {
                    mBundleRef.Remove(bundlePath);
                    UnloadAssetBundle(bundlePath, true);
                }
                return true;
            }
            return false;
        }

        public void CheckAssetRef(string bundlePath)
        {
            if (!mBundleRef.TryGetValue(bundlePath, out var refCount) || refCount == 0)
            {
                UnloadAssetBundle(bundlePath, true);
            }
        }

        private bool UnloadAssetBundle(string bundlePath, bool unloadAllLoadedObjects = false)
        {
            if (mBundleCache.TryGetValue(bundlePath, out var bundle))
            {
                if (bundle.IsDepAB) //一般不存在prefab包被依赖，除非嵌套引用，建议美术更改结构
                {
                    Debug.Log(string.Format("a prefab assetbundle[{0}] is depended by: {1}", bundlePath, bundle.GetDepsName()));
                    return false;
                }

                //解除所有依赖关系
                List<string> deps = GetDepList(bundle.ModName, bundle.BundleName);
                foreach (var dep in deps)
                {
                    var depPath = AssetLoader.GetBundlePath(bundle.ModName, dep);
                    if (mBundleCache.TryGetValue(depPath, out var depBundle))
                    {
                        if (depBundle.TryUnloadDep(bundlePath, unloadAllLoadedObjects, false))
                        {
                            mBundleCache.Remove(depPath);
                        }
                    }
                }
                bundle.Unload(unloadAllLoadedObjects, false);
                mBundleCache.Remove(bundlePath);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 强制卸载所有AB包，包括正在加载和将要加载的，一般用于切场景清理内存
        /// </summary>
        public void UnloadAllAssetBundle()
        {
            //清空延迟加载队列
            if (mDelayTaskQueue.Count > 0)
            {
                foreach (var loader in mDelayTaskQueue)
                {
                    loader.Discard();
                    loader.OnBundleLoadCallBack(null);
                }
                mDelayTaskQueue.Clear();
            }

            //终止所有正在加载的任务
            if (mCurLoadingBundle.Count > 0)
            {
                foreach (var loader in mCurLoadingBundle.Values)
                {
                    loader.Discard();
                }
                mCurLoadingBundle.Clear();
            }

            //卸载所有加载完成的AssetBundle
            if (mBundleCache.Count > 0)
            {
                foreach (var bundle in mBundleCache.Values)
                {
                    if (bundle != null)
                    {
                        bundle.Unload(true, true);
                    }
                }
                mBundleCache.Clear();
            }

            //清空依赖缓存列表
            mDepsListCache.Clear();
            //清空资源引用计数
            mBundleRef.Clear();
            //清除manifest
            mManifest.Clear();
        }

        public void SetLocalFileHandler(IAssetLocalFileHandler localFileHandler)
        {
            mLocalFileHandler = localFileHandler;
        }

        public void SetDownloadHandler(IAssetDownloadHandler downloadHandler)
        {
            mDownloadHandler = downloadHandler;
        }

        void Update()
        {
            if (mDelayTaskQueue.Count > 0)
            {
                while (mDelayTaskQueue.Count > 0)
                {
                    LoadAssetBundle(mDelayTaskQueue.Dequeue(), false);
                }
            }
        }

    }

}
