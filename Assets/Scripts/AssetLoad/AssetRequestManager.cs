using System;
using UnityEngine;
using System.Text;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Charlotte
{
    public partial class AssetRequestManager : MonoBehaviour
    {
        long curDownLoadingCount;     //当前下载队列数量
        int MaxDownloadingCount = 5; //最大允许下载数量
        static int isassetBundleEditor = -1;
        bool trackingAssetBundleState; //资源追踪状态
        const int maxLoadCount = 4; //最大进程加载数
        AssetCache cachesAsset; //缓存资源类
        AssetCache cachesAssetBundle; //缓存AssetBundle
        VersionConfig versionConfig = null; //版本控制类
        AssetBundleRequest assetBundleRequest; //预留空类
        Dictionary<string, GameObject> cachePrefab; //缓存预制体
        StringBuilder tmpPath = new StringBuilder(); //tepPath类
        List<string> trackedAssetBundles = new List<string>(); //追踪的AssetBundle列表
        HashSet<string> typeAssets = new HashSet<string>(new string[] { "prefab" }); //通用资源类型
        LinkedList<AssetAsyncRequest> assetRequests = new LinkedList<AssetAsyncRequest>(); //异步资源请求链表
        LinkedList<AssetBundleRequest> assetBundleList = new LinkedList<AssetBundleRequest>(); //assetBundle资源请求链表
        LinkedList<AssetAsyncRequest> asyncRequestsList = new LinkedList<AssetAsyncRequest>(); //异步请求器列表
        Dictionary<string, string> assetBundleExceptionInfos = new Dictionary<string, string>(); //assetBundle异常信息集合
        Dictionary<string, AssetBundleInfo> assetBundleMap = new Dictionary<string, AssetBundleInfo>(); //assetBundleInfo映射集合

        void Awake()
        {
            Init();
        }

        void Update()
        {
            UpdateAssetRequests();
        }

        public void Init()
        {
            cachesAsset = new AssetCache(100);
            cachesAssetBundle = new AssetCache(100);
            cachePrefab = new Dictionary<string, GameObject>();
            assetBundleRequest = new AssetBundleRequest();
            assetBundleRequest.assetBundleName = string.Empty;
            assetBundleRequest.isDone = true;
            //******************************************************************************//
            trackingAssetBundleState = false;
            Resources.UnloadUnusedAssets(); //卸载未使用的资源，释放从AB包中加载出来的资源
            StartCoroutine(MainTask()); //开始新线程执行任务
            //******************************************************************************//
        }
        
        IEnumerator MainTask() //主线程
        {
            yield return UpdateTask();
        }

        IEnumerator UpdateTask() //更新线程
        {
            while (true)
            {
                UpdateAssetBundleList();
                yield return null;
            }
        }
        
        #region Public Function
        public static bool IsassetBundleEditor 
        {
            get 
            {
#if UNITY_EDITOR
                if (AssetRequestManager.isassetBundleEditor == -1) 
                {
                    AssetRequestManager.isassetBundleEditor = EditorPrefs.GetBool("SimulateAssetBundle", true) ? 1 : 0;
                }
#endif
                return isassetBundleEditor == 1;
            }
            set 
            {
#if UNITY_EDITOR
                int newValue = value ? 1 : 0;
                if (newValue != isassetBundleEditor) 
                {
                    isassetBundleEditor = newValue;
                }
#endif
            }
        } //编译器模式MODE
        
        /// <summary>
        /// 异步加载
        /// </summary>
        /// <param name="assetName">资源名称</param>
        /// <param name="onFinishCallback">回调函数</param>
        /// <returns>AssetRequestBase</returns>
        public AssetRequestBase MainAsyncLoad(string assetName, Action<AssetRequestBase> callback = null) 
        {
            if (IsassetBundleEditor)
            {
                if (string.IsNullOrEmpty(assetName))
                {
                    if (callback != null)
                    {
                        try
                        {
                            callback(assetBundleRequest);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("LoadAsync exception:" + e.ToString());
                        }
                    }
                    return assetBundleRequest;
                }
                string assetBundleName, fileName;
                if (TryGetAssetBundleName(assetName, out assetBundleName, out fileName))
                    return _LoadAsync(assetBundleName, fileName, callback);
                else
                    return _LoadAsync(fileName, callback);
            }
            else
            {
                if (assetName.Contains("Streamloader")) //初始加载器
                {
                    int lastIdx = assetName.LastIndexOf('.');
                    string fname = assetName.Substring(0, lastIdx);
                    return _LoadAsync(fname, callback);
                }
                if (PathUtil.IsUIAsset(assetName)) //UI资源
                {
                    assetName = assetName.Replace("Assets/AssetBundle/", "");
                    string bundleName = GetUnityAssetbundleName(assetName);
                    string modName = PathUtil.UI_MOD_NAME;
                    return LoadAsyncAsset(modName, bundleName, assetName, callback);
                }
                if (PathUtil.IsCommonAsset(assetName)) //通用资源
                {
                    assetName = "assets/assetbundle/" + assetName;
                    string bundleName = GetUnityAssetbundleName(assetName);
                    string modName = PathUtil.MOD_NAME;
                    return LoadAsyncAsset(modName, bundleName, assetName, callback);
                }
                if (PathUtil.ISBattleAsset(assetName)) //战斗资源
                {
                    string bundleName = ReplacePrefabName(assetName);
                    string modeName = PathUtil.MOD_NAME;
                    return LoadAsyncAsset(modeName, bundleName, assetName, callback);
                }
                if (IsElseAsset(assetName)) //其他资源处理
                {
                    string bundleName = ReplacePrefabName(assetName);
                    string modeName = PathUtil.MOD_NAME;
                    return LoadAsyncAsset(modeName, bundleName, assetName, callback);
                }
                else
                {
                    return LoadAsyncCommonAsset(assetName, callback);
                }
            }
        }
        
        
        /// <summary>
        /// 通过AssetBundleTool异步加载资源
        /// </summary>
        /// <param name="modName">MOD名称</param>
        /// <param name="bundleName">bundle名称</param>
        /// <param name="assetName">资源名称</param>
        /// <param name="callback">回调函数</param>
        /// <returns></returns>
        public AssetRequestBase LoadAsyncAsset(string modName, string bundleName, string assetName, Action<AssetRequestBase> callback = null)
        {
            string assetNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(assetName);
            AssetBundleRequest req = new AssetBundleRequest();
            //通过AssetBundleTool加载
            AssetBundleTool.AssetLoader Loader = AssetBundleTool.AssetManager.LoadAsync(modName, bundleName, assetNameWithoutExt, (o) =>
            {
                req.asset = o.AssetObject;
                req.isDone = true;
                callback?.Invoke(req);
            });
            req.isDone = Loader.IsSuccess;
            req.assetName = assetName;
            req.bundlePath = Loader.BundlePath;
            req.onEndCallback = callback;
            return req;
        }
        
        /// <summary>
        /// 通过AssetBundleTool异步加载通用资源
        /// </summary>
        /// <param name="assetName">资源名称</param>
        /// <param name="callback"><回调函数/param>
        /// <returns></returns>
        public AssetRequestBase LoadAsyncCommonAsset(string assetName, Action<AssetRequestBase> callback = null)
        {
            string bundleName = GetUnityAssetbundleName(assetName);
            string modName = PathUtil.MOD_NAME; //通用MOD标签
            string assetNameWithoutExt = assetName.EndsWith(".unity") ? string.Empty : System.IO.Path.GetFileNameWithoutExtension(assetName);
            AssetBundleRequest req = new AssetBundleRequest();
            AssetBundleTool.AssetLoader Loader = AssetBundleTool.AssetManager.LoadAsync(modName, bundleName, assetNameWithoutExt, (o) =>
            {
                req.asset = o.AssetObject;
                req.bundle = o.Bundle;
                req.isDone = true;
                callback?.Invoke(req);
            });
            req.isDone = Loader.IsSuccess;
            req.assetName = assetName;
            req.onEndCallback = callback;
            return req;
        }
        
        /// <summary>
        /// 异步预加载预设体
        /// </summary>
        /// <param name="prefabName"></param>
        /// <param name="onFinishCallback"></param>
        public void PreloadPrefabAsync(string prefabName, Action callback = null)
        {
            MainAsyncLoad(prefabName, (AssetRequestBase req)=>
            {
                if (req.asset != null) 
                {
                    PutPrefabCache(prefabName, req.asset as GameObject);
                }
                callback?.Invoke();
            });

        }
        
        public string GetUnityAssetbundleName(string assetName)
        {
            string dirPath;
            if (assetName.EndsWith(".unity"))
            {
                dirPath = assetName.Replace(".unity", "");
            }
            else
            {
                dirPath = System.IO.Path.GetDirectoryName(assetName);
                if (dirPath.StartsWith("Assets/AssetBundle/"))
                {
                    dirPath = dirPath.Substring(0, 19);
                }
            }
            tmpPath.Clear();
            tmpPath.Append(dirPath.Replace('\\', '/')).Append(".assetbundles");
            return tmpPath.ToString().ToLower();
        }
        
        /// <summary>
        /// 异步加载预设体
        /// </summary>
        /// <param name="prefabName"></param>
        /// <param name="onFinishCallback"></param>
        public void LoadPrefabAsync(string prefabName, Action<GameObject> callback = null) 
        {
            GameObject pre = GetPrefabCache(prefabName);
            if (pre != null) 
            {
                callback(pre);
                return;
            }
            MainAsyncLoad(prefabName, (AssetRequestBase req)=>
            {
                GameObject prefab = null;
                if (req.asset != null) 
                {
                    var test = req.asset as GameObject;
                    if (test == null)
                        Debug.LogError("AssetRequestManager ->  LoadPrefabAsync " + prefabName + " Exception!!!");
                    else
                        prefab = Instantiate(test);
                }
                callback?.Invoke(prefab);
            });
        }
        
        /// <summary>
        /// 异步加载精灵
        /// </summary>
        /// <param name="spriteName">sprite名称</param>
        /// <param name="onFinishCallback">回调函数</param>
        public void LoadSpriteAsync(string spriteName, Action<Sprite> callback = null)
        {
            MainAsyncLoad(spriteName, (AssetRequestBase req) =>
            {
                Sprite sprite = null;
                if (req.asset != null)
                {
                    var texture = req.asset as Texture2D;
                    if (texture != null)
                    {
                        sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
                    }
                }
                callback?.Invoke(sprite);
            });
        }
        
        public string ReplacePrefabName(string assetName)
        {
            if (assetName.EndsWith(".prefab"))
                return assetName.Replace(".prefab", ".assetbundles");
            else
                return null;
        }
        
        /// <summary>
        /// 将预设体放入缓存
        /// </summary>
        /// <param name="key">缓存预设体Key</param>
        /// <param name="obj">对象</param>
        public void PutPrefabCache(string key, UnityEngine.GameObject obj)
        {
            if (obj != null) 
            {
                if (!cachePrefab.ContainsKey(key))
                {
                    cachePrefab.Add(key, obj);
                }
            }
        }
        
        /// <summary>
        /// 获取缓存中预设体
        /// </summary>
        /// <param name="key">缓存预设体Key</param>
        /// <returns></returns>
        GameObject GetPrefabCache(string key)
        {
            if (cachePrefab.ContainsKey(key)) 
            {
                var obj = cachePrefab[key];
                cachePrefab.Remove(key);
                var o = Instantiate(obj);
                return o;
            }
            return null;
        }
        
        /// <summary>
        /// 清空预设体缓存
        /// </summary>
        public void ClearPrefabCache() => cachePrefab.Clear();

        private bool TryGetAssetBundleName(string path, out string abName, out string fileName) 
        {
            path = path.Replace('\\', '/');
            if (path.StartsWith(PathUtil.AssetBundlePath)) 
            {
                path = path.Substring(PathUtil.AssetBundlePath.Length);
            }
            abName = PathUtil.FileToAssetBundleName(path);
            int lastIdx = path.LastIndexOf('.');
            if (lastIdx < 0) 
            {
                fileName = path;
                return false;
            }
            bool ret =
#if UNITY_EDITOR
            PathUtil.IsAssetBundleDir(path);
            if (!ret && !IsassetBundleEditor) 
            {
                ret = IsAssetBundleAsset(path);
            }
#else
            ret = IsAssetBundleAsset(path);
#endif
            if (ret)
                fileName = PathUtil.AssetBundlePath + path;
            else
                fileName = path.Substring(0, lastIdx);
            return ret;
        }
        
        /// <summary>
        /// AssetBundle加载
        /// </summary>
        /// <param name="assetBundleName">AssetBundle名称</param>
        /// <param name="callback">回调函数</param>
        /// <param name="isRelease">是否从不释放</param>
        /// <returns></returns>
        public AssetBundleInfo LoadAssetBundle(string assetBundleName,Action<AssetBundle> callback = null,bool isRelease = false)
        {
            #if UNITY_EDITOR
            if (IsassetBundleEditor) { return null; }
            #endif
            if (assetBundleName.EndsWith(".unity3d") && curDownLoadingCount >= MaxDownloadingCount)
            {
                return null;
            }
            Debug.Log("LoadAssetBundle -> " + assetBundleName);
            string[] dependencies = GetAllDependencies(assetBundleName);
            for (int i = 0; i < dependencies.Length; i++)
            {
                LoadAssetBundleInternal(assetBundleName, callback, isRelease);
            }
            var rootAsset = LoadAssetBundleInternal(assetBundleName, callback, isRelease, true);
            return rootAsset;
        }

        /// <summary>
        /// 加载AssetBundle本体与依赖
        /// </summary>
        /// <param name="assetBundleName">assetBundle名称</param>
        /// <param name="callback">回调函数</param>
        /// <param name="isRelease">是否从不释放</param>
        /// <param name="isRoot">是否是根节点本体</param>
        /// <returns></returns>
        public AssetBundleInfo LoadAssetBundleInternal(string assetBundleName, Action<AssetBundle> callback = null, bool isRelease = false, bool isRoot = false)
        {
            assetBundleName = assetBundleName.ToLower();
            if (trackingAssetBundleState)
            {
                trackedAssetBundles.Add(assetBundleName);
            }

            AssetBundleInfo loadedInfo;
            if (assetBundleMap.TryGetValue(assetBundleName.ToLower(), out loadedInfo))
            {
                loadedInfo.referenceCount++;
                loadedInfo.isRoot |= isRoot;
                if (callback != null)
                {
                    loadedInfo.onEndCallback += callback;
                }
                return loadedInfo;
            }

            curDownLoadingCount++;
            
            string fullPath = PathUtil.GetAssetBundleFullPath(assetBundleName);
            Hash128 hash = GetFileHash(fullPath);
            long fileSize = GetFileSize(fullPath);
            string strUrl = fullPath;
            if (PathUtil.IsUrlEncode)
            {
                strUrl = CommonTool.URLEncode(fullPath, Encoding.UTF8);
            }
#if !DOWNLOAD_HTTP
            var www = WWW.LoadFromCacheOrDownload(strUrl, hash);
            var info = new AssetBundleInfo();
            info.name = assetBundleName;
            info.www = www;
            www.threadPriority = ThreadPriority.BelowNormal;
            info.onEndCallback = callback;
#if TIME_STAMP
            //info.timeStamp = DateTime.Now.Ticks;
#endif
            info.isRoot = isRoot;
            info.needSave = false;
            info.referenceCount = 1;
            info.assetBundle = null;
            info.fileSize = fileSize;
            assetBundleMap[assetBundleName] = info;
#else
            var info = new AssetBundleInfo();
            info.name = assetBundleName;
            info.onFinishedCallback = funcCallback;
            info.referenceCount = 1;
            info.NeedToBeSave = false;
            info.assetBundle = null;
            info.isRoot = isRoot;
            info.URL = strUrl;

            mAssetBundleMap[assetBundleName] = info;
            StartCoroutine(DownloadAssetBundle(info));
            LoggerRelease.ForceOutputLog(string.Format("AssetBundle: LoadAsync started: {0} MD5 :{1}. ReqCount:{2}", info.name, hash, _downloadingReqCount));
#endif
            // if (OnDownloadFileInfoEvent != null)
            // {
            //     OnDownloadFileInfoEvent(strUrl, info.fileSize, www.progress, false);
            // }
            return info;
        }
        
        /// <summary>
        /// 取消异步加载
        /// </summary>
        public void CancelAsyncLoad(string path)
        {
            var node = assetRequests.First;
            while (node != null)
            {
                var next = node.Next;
                AssetAsyncRequest req = node.Value;
                if (req.assetName == path)
                {
                    assetRequests.Remove(node);
                }
                node = next;
            }
        }
        
        /// <summary>
        /// 取消全部异步加载
        /// </summary>
        public void CancelAllAsyncLoad()
        {
            var node = assetRequests.First;
            while (node != null)
            {
                var next = node.Next;
                assetRequests.Remove(node);
                node = next;
            }
        }
        
        #endregion
        
        #region EDITOR_MODE
        #if UNITY_EDITOR
                void UpdateAssetBundleListEditor() //更新编译器模式AssetBundlList
                {
                    var node = assetBundleList.First;
                    while (node != null)
                    {
                        var next = node.Next;
                        var req = node.Value;
                        var asset = LoadAssetEditor(req.assetBundleName, req.assetName); //编译器模式加载资源
                        req.asset = asset;
                        string cacheKey = req.assetBundleName + ":" + req.assetName; //缓存KEY
                        cachesAssetBundle.Insert(cacheKey,req.asset);
                        req.assetName = req.assetName.Substring(PathUtil.AssetBundlePath.Length);
                        req.isDone = true;
                        req.onEndCallback?.Invoke(req);
                        assetBundleList.Remove(node);
                        node = next;
                    }
                }
                
                /// <summary>
                /// 编译器模式资源加载
                /// </summary>
                /// <param name="assetBundleName">assetBundle名称</param>
                /// <param name="assetName">资源名</param>
                /// <returns></returns>
                private UnityEngine.Object LoadAssetEditor(string assetBundleName,string assetName) 
                {
                    var asset = AssetDatabase.LoadMainAssetAtPath(assetName);
                    if (asset != null)
                    {
                        return asset;
                    }
                    else
                    {
                        Debug.LogError("AssetRequestManager -> LoadAssetEditor:" + assetName + " | " + assetBundleName + " Exception!!!");
                        return null;
                    }
                }
        #endif
        #endregion
        
        #region Private Function
        /// <summary>
        /// 更新AssetBundle缓存列表
        /// </summary>
        void UpdateAssetBundleList()
        {
#if UNITY_EDITOR //编译器模式加载
            if (IsassetBundleEditor)
            {
                UpdateAssetBundleListEditor();
                return;
            }
#endif
            LinkedListNode<AssetBundleRequest> node = assetBundleList.First;
            while (node != null)
            {
                var next = node.Next;
                var req = node.Value;
                bool curNodeState = false; //当前节点删除状态
                if (req.assetBundleRequest == null)
                {
                    string error;
                    if (assetBundleExceptionInfos.TryGetValue(req.assetBundleName, out error))
                    {
                        curNodeState = true;
                    }
                    else
                    {
                        AssetBundleInfo info;
                        if (assetBundleMap.TryGetValue(req.assetBundleName.ToLower(),out info))
                        {
                            var assetBundle = info.assetBundle;
                            if (assetBundle != null)
                            {
                                req.assetBundleRequest = assetBundle.LoadAssetAsync(req.assetName);
                            }
                        }
                        else
                        {
                            LoadAssetBundle(req.assetBundleName);
                        }
                    }
                }
                else
                {
                    if (req.assetBundleRequest.isDone)
                    {
                        try
                        {
                            req.asset = req.assetBundleRequest.asset;
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e.ToString() + req.assetBundleName);
                        }

                        string cacheKey = req.assetBundleName + ":" + req.assetName;
                        cachesAssetBundle.Insert(cacheKey, req.asset);
                        req.assetName = req.assetName.Substring(PathUtil.AssetBundlePath.Length);
                        req.isDone = true;
                        if (req.onEndCallback != null)
                        {
                            try
                            {
                                req.onEndCallback(req);
                            }
                            catch (Exception e)
                            {
                                Debug.LogError(e.ToString());
                            }
                        }
                    }
                }
                
                if (curNodeState)
                {
                    assetBundleList.Remove(node);
                }
                
                node = next;
            }
        }
        
        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        UnityEngine.Object Load(string path)
        {
            var asset = cachesAsset.Get(path);
            if (asset == null)
            {
                asset = Resources.Load(path);
                if (asset != null)
                    cachesAsset.Insert(path,asset);
                else
                    Debug.LogWarning("AssetRequestManager -> Load:" + path + "Failed");
            }
            return asset;
        }
        
        /// <summary>
        /// 异步加载
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="callback">回调函数</param>
        /// <returns>AssetRequestBase</returns>
        AssetRequestBase LoadAsync(string path, Action<AssetRequestBase> callback = null)
        {
            AssetAsyncRequest req = new AssetAsyncRequest(path, callback); //构建子类
            UnityEngine.Object asset = null;
            asset = cachesAsset.Get(path); //从缓存资源类中获取
            if (asset != null)
            {
                req.asset = asset;
                req.isDone = true;
                if (callback != null)
                {
                    try
                    {
                        callback(req);
                    }
                    catch (Exception e)
                    {
                        Debug.Log("AssetRequestManager -> LoadAsync ->" + e.ToString());
                    }
                }
                return req;
            }
            else
            {
                var existedReq = GetLoadingAssetAsyncRequest(path);
                if (existedReq != null)
                {
                    existedReq.onEndCallback += callback;
                    return existedReq;
                }
                else
                {
                    var node = new LinkedListNode<AssetAsyncRequest>(req);
                    assetRequests.AddLast(node);
                    return req;
                }
            }
        }

        /// <summary>
        /// 异步加载
        /// </summary>
        /// <param name="assetBundleName">assetBundle名称</param>
        /// <param name="assetName">资源名称</param>
        /// <param name="callback">回调函数</param>
        /// <returns></returns>
        AssetRequestBase _LoadAsync(string assetBundleName, string assetName, Action<AssetRequestBase> callback = null) 
        {
            var req = new AssetBundleRequest();
            req.assetBundleName = assetBundleName;
            req.assetName = assetName;
            req.onEndCallback = callback;

            string cacheKey = assetBundleName + ":" + assetName;
            var asset = cachesAssetBundle.Get(cacheKey);
            if (asset != null) 
            {
                req.asset = asset;
                req.assetName = req.assetName.Substring(PathUtil.AssetBundlePath.Length);
                req.isDone = true;
                callback?.Invoke(req);
                return req;
            } 
            else 
            {
                var existedReq = GetAssetBundleRequest(assetBundleName, assetName);
                if (existedReq != null) 
                {
                    existedReq.onEndCallback += callback;
                    return existedReq;
                } 
                else 
                {
                    req.isDone = false;
                    var node = new LinkedListNode<AssetBundleRequest>(req);
                    assetBundleList.AddLast(node);
                    string fullPath = PathUtil.GetAssetBundleFullPath(assetBundleName);
                    curDownLoadingCount += GetFileSize(fullPath);
                    return req;
                }
            }
        }
        
        /// <summary>
        /// 异步加载
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="callback">回调函数</param>
        /// <returns></returns>
        AssetRequestBase _LoadAsync(string path, Action<AssetRequestBase> callback = null) 
        {
            AssetAsyncRequest req = new AssetAsyncRequest(path, callback);
            UnityEngine.Object asset = null;
            asset = cachesAsset.Get(path);
            if (asset != null) 
            {
                req.asset = asset;
                req.isDone = true;
                if (callback != null) 
                {
                    try 
                    {
                        callback(req);
                    }
                    catch(Exception e)
                    {
                        Debug.LogError("ResourceManager -> _LoadAsync exception: " + e.ToString());
                    }
                }
                return req;
            } 
            else 
            {
                var existedReq = GetAssetAsyncRequest(path);
                if (existedReq != null) 
                {
                    existedReq.onEndCallback += callback;
                    return existedReq;
                } 
                else 
                {
                    var node = new LinkedListNode<AssetAsyncRequest>(req);
                    asyncRequestsList.AddLast(node);
                    return req;
                }
            }
        }
        
        /// <summary>
        /// 获取异步资源请求器
        /// </summary>
        /// <param name="path">路径</param>
        /// <returns>AssetAsyncRequest</returns>
        AssetRequestBase GetAssetAsyncRequest(string path) 
        {
            var node = asyncRequestsList.First;
            while (node != null) 
            {
                var next = node.Next;
                AssetAsyncRequest req = node.Value;
                if (req.assetName == path) 
                {
                    return req;
                }
                node = next;
            }
            return null;
        }
        
        /// <summary>
        /// 获取AssetBundle请求
        /// </summary>
        /// <param name="assetBundleName"></param>
        /// <param name="assetName"></param>
        /// <returns></returns>
        AssetBundleRequest GetAssetBundleRequest(string assetBundleName, string assetName) 
        {
            var node = assetBundleList.First;
            while (node != null) 
            {
                var next = node.Next;
                var req = node.Value;
                if (req.assetBundleName == assetBundleName && req.assetName == assetName) 
                {
                    return req;
                }
                node = next;
            }
            return null;
        }
        
        /// <summary>
        /// 获取异步资源子类
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        AssetAsyncRequest GetLoadingAssetAsyncRequest(string path)
        {
            var node = assetRequests.First;
            while (node != null)
            {
                var next = node.Next;
                AssetAsyncRequest req = node.Value;
                if (req.assetName == path)
                {
                    return req;
                }
                node = next;
            }
            return null;
        }

        /// <summary>
        /// 更新资源加载
        /// </summary>
        void UpdateAssetRequests()
        {
            if (assetRequests.Count > 0)
            {
                string name = assetRequests.First.Value.assetName;
            }
            int LoadCount = 0;
            var node = assetRequests.First;
            while (node != null)
            {
                var next = node.Next;
                AssetAsyncRequest req = node.Value;
                if (req.unityRequest != null) //已经开始加载
                {
                    if (req.unityRequest.isDone) //判断是否加载完成
                    {
                        req.isDone = true;
                        var asset = req.asset = req.unityRequest.asset;
                        if (asset != null)
                        {
                            if (!req.isCache)
                            {
                                cachesAsset.Insert(req.assetName, asset); //放入缓存
                            }
                        }
                        else
                            Debug.LogWarning("AssetRequestManager -> UpdateAssetRequests -> Load:" + req.assetName + "Failed");
                        
                        var finishedCallback = req.onEndCallback;
                        if (finishedCallback != null)
                        {
                            try
                            {
                                finishedCallback(req);
                            }
                            catch (Exception e)
                            {
                                Debug.LogError(e);
                            }
                        }

                        assetRequests.Remove(node);
                    }
                    else
                        LoadCount++;
                }
                else //未开始加载
                {
                    var asset = cachesAsset.Get(req.assetName);
                    if (asset != null)
                    {
                        req.asset = asset;
                        req.isDone = true;
                        if (req.onEndCallback != null)
                        {
                            try
                            {
                                req.onEndCallback(req);
                            }
                            catch (Exception e)
                            {
                                Debug.LogError(e);
                            }
                        }
                        assetRequests.Remove(node);
                    }
                    else
                    {
                        if (LoadCount < maxLoadCount)
                        {
                            req.unityRequest = Resources.LoadAsync(req.assetName);
                            LoadCount++;
                        }
                    }
                    
                    node = next;
                }
            }
        }
        
        /// <summary>
        /// 获取assetBundle Json依赖
        /// </summary>
        /// <returns></returns>
        string[] GetAllDependencies(string assetBundleName)
        {
            for (int i = 0; i < bundleJsonList.bundle.Count; i++)
            {
                if (bundleJsonList.bundle[i].f == assetBundleName)
                {
                    return bundleJsonList.bundle[i].d;
                }
            }
            return new string[0];
        }
        
        /// <summary>
        /// 获取文件哈希码
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>Hash128</returns>
        Hash128 GetFileHash(string filePath) 
        {
            if (versionConfig != null)
                return versionConfig.GetHash(filePath);
            else
                return new Hash128(0, 0, 0, 0);
        }
        
        /// <summary>
        /// 获取文件大小
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>long</returns>
        long GetFileSize(string filePath)
        {
            if (versionConfig != null)
                return versionConfig.GetFileSize(filePath);
            else
                return 0;
        }
        
        /// <summary>
        /// 判断AssetBundle资源
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        bool IsAssetBundleAsset(string path) 
        {
#if UNITY_EDITOR
            if (IsassetBundleEditor) 
            {
                string abName = PathUtil.FileToAssetBundleName(path);
                string[] allAssets = AssetDatabase.GetAssetPathsFromAssetBundle(abName.ToLower());
                string abFile = (PathUtil.AssetBundlePath + path).ToLower();
                for (int i = 0; i < allAssets.Length; i++) 
                {
                    if (allAssets[i].ToLower() == abFile) 
                    {
                        return true;
                    }
                }
                return false;
            }
#endif
            var assetBundleName = PathUtil.FileToAssetBundleName(path);
            Debug.Log("assetBundleName: " + assetBundleName);
            if (string.IsNullOrEmpty(assetBundleName)) 
            {
                return false;
            }
            if (!IsassetBundleEditor) 
            {
                Debug.LogError("AssetBundle未初始化");
                return false;
            } 
            else 
            {
                Debug.Log("AssetBundle未初始化 ELSE !!!");
            }
            return false;
        }
        
        /// <summary>
        /// 判断其他资源
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public bool IsElseAsset(string assetName)
        {
            string dirPath = System.IO.Path.GetDirectoryName(assetName);
            dirPath = dirPath.Replace('\\', '/');
            int index = dirPath.IndexOf('/');
            if (index > 0)
                dirPath = dirPath.Substring(0, index);
            if (typeAssets.Contains(dirPath))
            {
                return true;
            }
            return false;
        }
        #endregion
        
        
        BundleJsonList bundleJsonList;//data类
        internal struct BundleJson //JsonData接口
        {
            public string f; //文件
            public string[] d; //依赖
        }
        internal struct BundleJsonList //JsonList接口
        {
            public List<BundleJson> bundle; //JsonList
        }
        
        public string AssetsBuildPath = "Assets/streamingassets/standalonewindows/";
        /*
    #if UNITY_ANDROID
                                    "android/";
    #elif UNITY_EDITOR
                                    "standalonewindows/";
    #endif
        */
    }
}