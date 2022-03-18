using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetBundleTool
{
    public class AssetLoader
    {
        public string ModName { get; private set; }
        public string BundleName { get; private set; }
        public string BundlePath { get; private set; }
        public string AssetName { get; private set; }
        public bool IsAsync { get; private set; }
        private Action<AssetLoader> mCallback;

        public AssetBundleTool.AssetBundle Bundle { get; private set; }
        public Object AssetObject { get; private set; }

        public bool IsDone { get; private set; }
        public bool IsDiscard { get; private set; }

        private Action<AssetBundle> mABLoadCallback;

        private const string DEP_KEY = "DepChild=";

        private static Dictionary<string, string> mAssetNameDict = new Dictionary<string, string>();

        //多个请求加载同一个物件的加载队列
        private Queue<AssetLoader> mLinkedLoader;
        private List<string> mDepList;
        private int mCurLoadDepIndex;
        
        private IAssetDownloadHandler mDownloadHandler;
        private IAssetLocalFileHandler mLocalFileHandler;

        public AssetLoader(string modName, string bundleName, string assetName, bool isAsync, Action<AssetLoader> callback)
        {
            this.ModName = modName;
            this.BundleName = bundleName.ToLower();
            if (modName == string.Empty)  //空字符串表示忽略mod名(用于bundleName里已经包含mod的情况，或者加载的是根目录的资源)
            {
                this.BundlePath = BundleName;

                //尝试从BundleName中解析monName
                if (bundleName.StartsWith("mod_"))
                {
                    int index = BundleName.IndexOf('/');
                    this.ModName = BundleName.Substring(0, index);
                    this.BundleName = BundleName.Substring(index + 1);
                }
            }
            else
            {
                this.BundlePath = GetBundlePath(ModName, BundleName);
            }
            this.AssetName = assetName == null ? BundleNameToAssetName(bundleName) : assetName;
            this.IsAsync = isAsync;
            this.mCallback = callback;
        }

        public override string ToString()
        {
            string name = IsDepAB ? BundlePath + "(Dep)" : BundlePath;
            string ret = IsSuccess ? "Success" : "Failed";
            return string.Format("[Load {0} {1}] ", name, ret);
        }

        public bool IsSuccess
        {
            get
            {
                if (IsDepAB || AssetName == string.Empty)
                    return Bundle != null;
                return AssetObject != null;
            }
        }

        public bool IsDepAB
        {
            get
            {
                if (Bundle != null && Bundle.IsDepAB)
                    return true;
                return AssetName.StartsWith(DEP_KEY);
            }
        }

        public static string GetBundlePath(string modName, string bundleName)
        {
            return string.Format("{0}/{1}", modName, bundleName);
        }

        /// <summary>
        /// 加载过程中调用，取消加载
        /// </summary>
        public void Discard()
        {
            IsDiscard = true;
            if (mDownloadHandler != null)
            {
                mDownloadHandler.CancelDownload(ModName, BundleName, AssetName);
            }
        }

        public string BundleNameToAssetName(string bundleName)
        {
            if (!mAssetNameDict.TryGetValue(bundleName, out var assetName))
            {
                assetName = Path.GetFileNameWithoutExtension(bundleName);
                mAssetNameDict[bundleName] = assetName;
            }

            return assetName;
        }

        /// <summary>
        /// 加入等待队列, 加载完毕后调用该loader的CallBack
        /// </summary>
        /// <returns></returns>
        public void AddLinkedLoader(AssetLoader loader)
        {
            if (mLinkedLoader == null)
            {
                mLinkedLoader = new Queue<AssetLoader>();
            }
            mLinkedLoader.Enqueue(loader);
        }

        public void StartLoading(List<string> depList, Action<AssetBundle> callback = null)
        {
            mDepList = depList;
            mCurLoadDepIndex = 0;
            mABLoadCallback = callback;
            LoadNext();
        }

        private void LoadNext()
        {
            if (mDepList != null && mCurLoadDepIndex < mDepList.Count)  //加载依赖
            {
                if (IsAsync)
                {
                    AssetManager.LoadAsync(this.ModName, mDepList[mCurLoadDepIndex++], DEP_KEY + BundlePath, (loader) =>
                    {
                        if (!loader.IsSuccess)
                        {
                            Debug.LogError(this.BundlePath + loader);
                        }
                        LoadNext();
                    });
                }
                else
                {
                    var loader = AssetManager.Load(this.ModName, mDepList[mCurLoadDepIndex++], DEP_KEY + BundlePath);
                    if (!loader.IsSuccess)
                    {
                        Debug.LogError(this.BundlePath + loader);
                    }
                    LoadNext();
                }
            }
            else //加载本体
            {
                if (CheckExists(out var root_path))
                {
                    if (IsAsync)
                    {
                        AssetManager.Instance.StartCoroutine(StartLoadAssetBundle(root_path));
                    }
                    else
                    {
                        var bundle = UnityEngine.AssetBundle.LoadFromFile(root_path + BundlePath);
                        OnLoadBundleFinish(bundle);
                    }
                }
            }
        }

        IEnumerator StartLoadAssetBundle(string root_path)
        {
            var bundleLoadRequest = UnityEngine.AssetBundle.LoadFromFileAsync(root_path + BundlePath);
            while (!bundleLoadRequest.isDone)
            {
                yield return null;
            }
            OnLoadBundleFinish(bundleLoadRequest.assetBundle);
        }

        private bool CheckExists(out string root_path)
        {
            root_path = null;

            if (File.Exists(AssetManager.EXPAND_PATH + BundlePath))   //先检查可读写路径
            {
                root_path = AssetManager.EXPAND_PATH;
                return true;
            }
            if (mLocalFileHandler != null)  //再检查本地路径
            {
                if(mLocalFileHandler.Exists(AssetManager.LOCAL_PATH + BundlePath))
                {
                    root_path = AssetManager.LOCAL_PATH;
                    return true;
                }
            }
            else if (File.Exists(AssetManager.LOCAL_PATH + BundlePath))  //此接口真机下不支持读取apk包内资源
            {
                root_path = AssetManager.LOCAL_PATH;
                return true;
            }

            //本地资源都没有找到，尝试从网络下载资源
            if (mDownloadHandler != null)
            {
                if (IsAsync)
                {
                    mDownloadHandler.RequestDownloadAsync(ModName, BundleName, AssetName, (ab) =>
                    {
                        OnLoadBundleFinish(ab);
                    });
                }
                else
                {
                    var dlBundle = mDownloadHandler.RequestDownload(ModName, BundleName, AssetName);
                    OnLoadBundleFinish(dlBundle);
                }
            }
            else //没有实现下载接口，标记为加载失败
            {
                OnLoadBundleFinish(null);
            }

            return false;
        }

        private void OnLoadBundleFinish(UnityEngine.AssetBundle assetBundle)
        {
            AssetBundle bundle = null;
            if (assetBundle != null)
            {
                bundle = new AssetBundle(ModName, BundleName, assetBundle);
            }
            OnBundleLoadCallBack(bundle);
        }

        /// <summary>
        /// 加载AB包回调
        /// </summary>
        /// <param name="bundle"></param>
        public void OnBundleLoadCallBack(AssetBundle bundle)
        {
            //回调给manager，解除任务状态
            if (mABLoadCallback != null)
            {
                mABLoadCallback.Invoke(bundle);
            }

            //处理自己的加载完成逻辑
            if (bundle == null)
            {
                OnLoadError("AssetBundle Load Error ");
            }
            else
            {
                this.Bundle = bundle;
                if (IsDiscard)  //加载的包已被丢弃，直接完成
                {
                    OnLoadFinish();
                }
                else if(AssetName == string.Empty)  //不加载assets
                {
                    OnLoadFinish();
                }
                else if (AssetName.StartsWith(DEP_KEY))  //加载的是依赖包，添加依赖关系，不加载Asset
                {
                    string childAB = AssetName.Substring(DEP_KEY.Length);
                    bundle.AddDepChild(childAB);
                    OnLoadFinish();
                }
                else //加载Asset资源
                {
                    bundle.GetAsset(AssetName, IsAsync, OnAssetLoadCallBack);
                }
            }

            //派发所有的linked loader
            if (mLinkedLoader != null)
            {
                while (mLinkedLoader.Count > 0)
                {
                    var loader = mLinkedLoader.Dequeue();
                    loader.OnBundleLoadCallBack(bundle);
                }
            }
        }

        /// <summary>
        /// 加载Asset回调
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isLoadOk"></param>
        /// <param name="o"></param>
        public void OnAssetLoadCallBack(string name, bool isLoadOk, Object o)
        {
            if (!isLoadOk)
            {
                OnLoadError("Asset Load Error");
            }
            else
            {
                OnLoadSuccess(o);
            }
        }

        private void OnLoadError(string errMessage)
        {
            Debug.LogError(this + errMessage);
            OnLoadFinish();
        }

        private void OnLoadSuccess(Object obj)
        {
            AssetObject = obj;
            OnLoadFinish();
        }

        private void OnLoadFinish()
        {
            IsDone = true;
            if (IsDiscard)
            {
                if (AssetObject != null)
                {
                    Object.Destroy(AssetObject);
                    AssetObject = null;
                }
                AssetManager.Instance.CheckAssetRef(BundlePath);
            }
            else if (mCallback != null)
            {
                mCallback.Invoke(this);
                mCallback = null;
            }
        }

        public void SetLocalFileHandler(IAssetLocalFileHandler localFileHandler)
        {
            mLocalFileHandler = localFileHandler;
        }

        public void SetDownloadHandler(IAssetDownloadHandler downloadHandler)
        {
            mDownloadHandler = downloadHandler;
        }

    }

}
