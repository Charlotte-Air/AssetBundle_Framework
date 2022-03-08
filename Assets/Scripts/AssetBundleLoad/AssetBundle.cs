using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleTool
{
    public class AssetBundle
    {

        private HashSet<string> mChildAB = new HashSet<string>();

        //一个bundle对应多个物件(一般1个prefab1个包，这里只是做下支持，便于扩展)
        private Dictionary<string, AssetInfo> mAssets = new Dictionary<string, AssetInfo>();

        public string ModName { get; private set; }
        public string BundleName { get; private set; }
        public UnityEngine.AssetBundle Bundle { get; private set; }
        
        public delegate void LoadResCallBack(string assetName, bool isLoadOK, Object asset);
        private Dictionary<string, Queue<ABCallBackHandler>> mLoadCallbackList = new Dictionary<string, Queue<ABCallBackHandler>>();

        public class ABCallBackHandler
        {
            private string mAssetName;
            private LoadResCallBack mCallBack;

            public ABCallBackHandler(string assetName, LoadResCallBack callback)
            {
                this.mAssetName = assetName;
                this.mCallBack = callback;
            }

            public void DoCallback(bool isLoadOK, Object asset)
            {
                if (mCallBack != null)
                {
                    mCallBack(mAssetName, isLoadOK, asset);
                }
            }

        }

        class AssetInfo
        {

            public string Name { get; private set; }
            public Object Asset { get; private set; }
            public LoadStatus Status { get; set; }

            public AssetInfo(string name)
            {
                this.Name = name;
                this.Status = LoadStatus.LoadingAsset;
            }

            public void LoadFinish(Object asset)
            {
                this.Asset = asset;
                this.Status = LoadStatus.LoadAssetDone;
            }

            public enum LoadStatus
            {
                LoadingAsset,
                LoadAssetDone,
            }
        }

        public AssetBundle(string modName, string bundleName, UnityEngine.AssetBundle assetBundle)
        {
            this.ModName = modName;
            this.BundleName = bundleName;
            this.Bundle = assetBundle;
        }

        public bool IsDepAB
        {
            get { return mChildAB.Count > 0; }
        }

        public string GetDepsName()
        {
            if (IsDepAB)
            {
                string names = "";
                foreach (var name in mChildAB)
                {
                    names += name + ' ';
                }
            }
            return "";
        }

        public void AddDepChild(string childAB)
        {
            if (childAB != null)
            {
                mChildAB.Add(childAB);
            }
        }

        public void GetAsset(string assetName, bool isAsync, LoadResCallBack callback)
        {
            if (mAssets.TryGetValue(assetName, out var assetInfo))
            {
                if (assetInfo.Status == AssetInfo.LoadStatus.LoadAssetDone) //已经加载完成，无论对错，直接返回
                {
                    callback(assetName, assetInfo.Asset != null, assetInfo.Asset);
                }
                else if (assetInfo.Status == AssetInfo.LoadStatus.LoadingAsset) //正在加载中
                {
                    if (isAsync) //异步加载，添加到同一处理进程
                        AddCallbackList(assetName, callback);
                    else //同步加载，立即执行(返回的东西跟异步的一定是同一个，所以不需要维护)
                        LoadAsset(new AssetInfo(assetName), isAsync, callback);
                }
            }
            else
            {
                var newAssetInfo = new AssetInfo(assetName);
                mAssets.Add(assetName, newAssetInfo);
                LoadAsset(newAssetInfo, isAsync, callback);
            }
        }

        private void LoadAsset(AssetInfo assetInfo, bool isAsync, LoadResCallBack callback)
        {
            if (isAsync)
            {
                AddCallbackList(assetInfo.Name, callback);
                AssetManager.Instance.StartCoroutine(StartLoadAsset(assetInfo));
            }
            else
            {
                var asset = Bundle.LoadAsset(assetInfo.Name);
                assetInfo.LoadFinish(asset);
                callback(assetInfo.Name, asset != null, asset);
            }
        }

        private void AddCallbackList(string assetName, LoadResCallBack callback)
        {
            if (!mLoadCallbackList.TryGetValue(assetName, out var list))
            {
                list = new Queue<ABCallBackHandler>();
                mLoadCallbackList.Add(assetName, list);
            }
            ABCallBackHandler info = new ABCallBackHandler(assetName, callback);
            list.Enqueue(info);
        }

        IEnumerator StartLoadAsset(AssetInfo assetInfo)
        {
            var abRequest = Bundle.LoadAssetAsync(assetInfo.Name);
            while (!abRequest.isDone)
            {
                yield return null;
            }
            if (Bundle.name.Contains("PersonalInfo"))
            {
                Debug.LogError("catch");
            }
            Object asset = abRequest.asset;
            assetInfo.LoadFinish(asset);
            OnAssetLoadFinish(assetInfo.Name, asset);
        }

        private void OnAssetLoadFinish(string assetName, Object asset)
        {
            if (mLoadCallbackList.TryGetValue(assetName, out var list))
            {
                while (list.Count > 0)
                {
                    var info = list.Dequeue();
                    info.DoCallback(asset != null, asset);
                }
            }
        }

        public bool TryUnloadDep(string child, bool unloadAllLoadedObjects, bool force = false)
        {
            if (mChildAB.Remove(child))
            {
                if (mChildAB.Count < 1)
                {
                    return Unload(unloadAllLoadedObjects, force);
                }
            }
            return false;
        }

        public bool Unload(bool unloadAllLoadedObjects, bool force = false)
        {
            if (!force)
            {
                if (mChildAB.Count > 0)   //如果还有依赖，不能卸载
                {
                    return false;
                }

                foreach (var info in mAssets.Values)
                {
                    if (info.Status == AssetInfo.LoadStatus.LoadingAsset)   //正在加载中，不能卸载
                    {
                        return false;
                    }
                }
            }
            
            mAssets.Clear();
            Bundle.Unload(unloadAllLoadedObjects);
            Bundle = null;
            mChildAB.Clear();
            mLoadCallbackList.Clear();
            return true;
        }

    }

}
