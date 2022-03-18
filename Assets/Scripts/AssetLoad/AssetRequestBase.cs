using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Charlotte
{
    public class AssetBundleInfo
    {
        public WWW www; //Url
        public string name; //名称
        public long fileSize; //文件大小
        public bool needSave; //保存状态
        public bool isRelease; //释放状态
        public bool isManifest;//显示状态
        public bool isRoot; //资源节点状态
        public bool isLoadFinish; //加载状态
        public int referenceCount; //引用数量
        public UnityEngine.AssetBundle assetBundle; //UnityEngine本体
        public Action<UnityEngine.AssetBundle> onEndCallback; //结束回调
    }
    
    public abstract class AssetRequestBase : IEnumerator //资源请求基类(可迭代)
    {
        public bool isDone; //是否加载完
        public string assetName; //资源名称
        public string bundlePath;  //Bundle路径
        public UnityEngine.Object asset;  //资源对象
        public AssetBundleTool.AssetBundle bundle; //AssetBundle加载器
        public Action<AssetRequestBase> onEndCallback; //加载完成回调
        public void Reset() { }
        public bool MoveNext() => !isDone;
        public object Current { get { return null; } }
        public virtual string GetAssetName() => string.Empty;
    }
    
    public class AssetBundleRequest : AssetRequestBase //AssetBundle请求器子类
    {
        public string assetBundleName;
        public UnityEngine.AssetBundleRequest assetBundleRequest;
        public override string GetAssetName()
        {
            int idx = assetName.LastIndexOf('.');
            return idx >= 0 ? assetName.Substring(0, idx) : assetName;
        }
    }
    
    public class AssetAsyncRequest : AssetRequestBase  //异步资源请求器
    {
        public bool isCache; //缓存状态
        public UnityEngine.ResourceRequest unityRequest; //UnityEngine Request
        /// <summary>
        /// 构建异步加载器
        /// </summary>
        /// <param name="path">资源地址</param>
        /// <param name="callback">回调函数</param>
        public AssetAsyncRequest(string path, Action<AssetRequestBase> callback)
        {
            asset = null;
            isDone = false;
            assetName = path;
            unityRequest = null;
            onEndCallback = callback;
        }
        public override string GetAssetName() => assetName;
    }

    public class AssetCache //资源缓存
    {
        int cacheSize; //缓存大小
        LinkedList<AssetCacheNode> cacheList; //缓存节点列表
        Dictionary<string, AssetCacheNode> cacheMap;  //缓存信息映射
        class AssetCacheNode //缓存节点
        {
            public string key; //资源主KEY
            public UnityEngine.Object asset; //Unity Objet资源
            public LinkedListNode<AssetCacheNode> nodeInCaches; //该节点内的缓存列表
        }
        
        public AssetCache(int size)
        {
            cacheSize = size;
            cacheList = new LinkedList<AssetCacheNode>();
            cacheMap = new Dictionary<string, AssetCacheNode>();
        }

        /// <summary>
        /// 获取缓存UnityEngine Object
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public UnityEngine.Object Get(string path)
        {
            AssetCacheNode node;
            bool isCache = cacheMap.TryGetValue(path, out node);
            if (isCache) //判断是否存在缓存映射(存在将将原先的节点删除，插入在头部）
            {
                cacheList.Remove(node.nodeInCaches);
                cacheList.AddFirst(node.nodeInCaches);
                return node.asset;
            }
            else
                return null;
        }

        /// <summary>
        /// 插入资源在缓存中
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="asset">UnityEngine Object资源</param>
        public void Insert(string path, UnityEngine.Object asset)
        {
            if (cacheList.Count >= cacheSize)//如果链表数量超过缓存大小则剔除链表最后节点资源
            {
                var finallyNode = cacheList.Last;
                cacheMap.Remove(finallyNode.Value.key);
                cacheList.Remove(finallyNode);
            }
            //如果没有超过缓存大小将进行插入
            AssetCacheNode insertCacheNode = new AssetCacheNode();
            var insertNodeList = new LinkedListNode<AssetCacheNode>(insertCacheNode);
            insertCacheNode.key = path;
            insertCacheNode.asset = asset;
            insertCacheNode.nodeInCaches = insertNodeList;
            cacheMap.Add(insertCacheNode.key, insertCacheNode);
            cacheList.AddFirst(insertNodeList);
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        public void Clear()
        {
            cacheMap.Clear();
            cacheList.Clear();
        }
    }
    
    public class AssetBundlePack
    {
        public enum PackType 
        {
            None,
            Windows,
            iOS,
            Android,
            WebPlayer
        }
        public static string GetDirName(PackType type) => type.ToString();
        public static string GetName(string strType) => "AssetBundles_" + strType;
        public static string GetName(PackType type) => "AssetBundles_" + type.ToString();
        public static string GetPackName(PackType type, int iVer) => GetName(type) + "_v" + iVer.ToString() + ".zip";
        public static string GetPackName(PackType type, int iVer, int idx) => GetName(type) + "_v" + iVer.ToString() + "." + idx.ToString() + ".zip";
    }
}