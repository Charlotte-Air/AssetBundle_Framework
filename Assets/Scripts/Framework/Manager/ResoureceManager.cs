using System;
using System.IO;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ResoureceManager : MonoBehaviour
{
    Dictionary<string, BundleInfo> BundleInfos = new Dictionary<string, BundleInfo>();
    Dictionary<string, BundlData> assetBundles = new Dictionary<string, BundlData>();
    internal class BundleInfo
    {
        public string AssetsName;
        public string BundleName;
        public List<string> Dependences;
    }
    
    internal class BundlData
    {
        public AssetBundle Bundle;
        public int Count; //引用计数
        public BundlData(AssetBundle ab)
        {
            Bundle = ab;
            Count = 1;
        }
    }

    /// <summary>
    /// 解析版本文件
    /// </summary>
    public void ParseVersionFile()
    {
        string url = Path.Combine(PathUtil.BundleResourcePath, AppConst.FileListName); //取得版本文件路径
        string[] data = File.ReadAllLines(url); //读取每一行
        for (int i = 0; i < data.Length; i++) //解析文件信息
        {
            BundleInfo bundeInfo = new BundleInfo();
            string[] info = data[i].Split('|');
            bundeInfo.AssetsName = info[0];
            bundeInfo.BundleName = info[1];
            bundeInfo.Dependences = new List<string>(info.Length - 2); //动态扩展
            for (int j = 2; j < info.Length; j++)
            {
                bundeInfo.Dependences.Add(info[j]);
            }
            BundleInfos.Add(bundeInfo.AssetsName,bundeInfo);

            if (info[0].IndexOf("LuaScripts") > 0) //查找Lua文件夹
            {
                Manager.Lua.LuaNames.Add(info[0]);
            }
        }
    }

    /// <summary>
    /// 异步加载资源
    /// </summary>
    /// <param name="assetsName">资源名</param>
    /// <param name="action">回调</param>
    IEnumerator LoadBundleAsync(string assetsName,Action<UnityEngine.Object> action = null)
    {
        if (!BundleInfos.ContainsKey(assetsName))
        {
            yield break;
        }

        string bundleName = BundleInfos[assetsName].BundleName;
        string bundlePath = Path.Combine(PathUtil.BundleResourcePath, bundleName);
        List<string> dependences = BundleInfos[assetsName].Dependences;

        BundlData bundle = GetBundle(bundleName);
        if (bundle == null)
        {
            UnityEngine.Object obj = Manager.Pool.TakeObject("AssestBundle", bundleName); //取对象池Bundle
            if (obj != null)
            {
                AssetBundle ab = obj as AssetBundle;
                bundle = new BundlData(ab);
            }
            else
            {
                AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(bundlePath); //加载资源AB包
                yield return request;
                bundle = new BundlData(request.assetBundle);
            }
            assetBundles.Add(bundleName, bundle);
        }

        if (dependences != null && dependences.Count > 0) //检测依赖资源
        {
            for (int i = 0; i < dependences.Count; i++)
            {
                yield return LoadBundleAsync(dependences[i]);  //依赖资源加载
            }
        }

        if (assetsName.EndsWith(".unity"))  //Packge Builde模式下屏蔽加载 
        {
            action?.Invoke(null);
            yield break;
        }

        if (action == null) //当依赖资源时退出循环
        {
            yield break;
        }

        AssetBundleRequest bundleRequest = bundle.Bundle.LoadAssetAsync(assetsName); //加载资源
        yield return bundleRequest;
        Debug.Log("->PackgeBundleLoadAssest");
        action?.Invoke(bundleRequest?.asset);
    }

    /// <summary>
    /// 获取BundleData
    /// </summary>
    /// <param name="name">资源名</param>
    /// <returns></returns>
    BundlData GetBundle(string name)
    {
        BundlData bundle = null;
        if (assetBundles.TryGetValue(name, out bundle))
        {
            bundle.Count++;
            return bundle;
        }
        return null;
    }

    /// <summary>
    /// 减去Bundle依赖引用计数
    /// </summary>
    /// <param name="assetsName">资源名</param>
    public void MinusBundleCount(string assetsName)
    {
        string bundleName = BundleInfos[assetsName].BundleName;
        MinusOneBundleCount(bundleName);
        List<string> dependences = BundleInfos[assetsName].Dependences;
        if (dependences != null)
        {
            var en = dependences.GetEnumerator();
            while (en.MoveNext())
            {
                string name = BundleInfos[en.Current].BundleName;
                MinusOneBundleCount(name);
            }
            // foreach (string key in dependences)
            // {
            //     string name = BundleInfos[key].BundleName;
            //     MinusOneBundleCount(name);
            // }
        }
    }

    void MinusOneBundleCount(string bundleName)
    {
        if(assetBundles.TryGetValue(bundleName,out BundlData bundldata))
        {
            if (bundldata.Count > 0)
            {
                bundldata.Count--;
                Debug.LogFormat("{0}引用数:{1}", bundleName, bundldata.Count);
            }
            if (bundldata.Count <= 0)
            {
                Debug.LogFormat("{0}放入Bundle对象池", bundleName);
                Manager.Pool.RecycleObject("AssetsBundle", bundleName, bundldata.Bundle);
                assetBundles.Remove(bundleName);
            }
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编译器环境加载资源
    /// </summary>
    /// <param name="assestName">资源名</param>
    /// <param name="action"></param>
    void EditorLoadAssest(string assestName, Action<UnityEngine.Object> action = null)
    {
        Debug.Log("->EditorLoadAssest");
        UnityEngine.Object obj = UnityEditor.AssetDatabase.LoadAssetAtPath(assestName, typeof(UnityEngine.Object));
        if (obj != null)
            action?.Invoke(obj);
        else
            Debug.LogError("Assest Name is Not Exist:" + assestName);
    }
#endif


    /// <summary>
    ///  加载资源
    /// </summary>
    /// <param name="assestName">资源名</param>
    /// <param name="action">回调</param>
    void LoadAssest(string assestName, Action<UnityEngine.Object> action)
    {
#if UNITY_EDITOR //避免Build出错
        if (AppConst.gameMode == GameMode.EditorMode)
            EditorLoadAssest(assestName, action);
#endif
        if (AppConst.gameMode != GameMode.EditorMode)
            StartCoroutine(LoadBundleAsync(assestName, action));
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    /// <param name="name">资源名</param>
    public void ReleaseBundle(UnityEngine.Object obj)
    {
        AssetBundle ab =  obj as AssetBundle;
        ab.Unload(true);
    }


    #region 加载接口
    /// <summary>
    /// 加载UI
    /// </summary>
    /// <param name="assetsName">资源名</param>
    /// <param name="action">回调</param>
    public void LoadUI(string assetsName, Action<UnityEngine.Object> action = null) => LoadAssest(PathUtil.GetUIPath(assetsName), action);

    /// <summary>
    /// 加载音乐
    /// </summary>
    /// <param name="assetsName">资源名</param>
    /// <param name="action">回调</param>
    public void LoadMusic(string assetsName, Action<UnityEngine.Object> action = null) => LoadAssest(PathUtil.GetMusicPath(assetsName), action);

    /// <summary>
    /// 加载音效
    /// </summary>
    /// <param name="assetsName">资源名</param>
    /// <param name="action">回调</param>
    public void LoadSound(string assetsName, Action<UnityEngine.Object> action = null) => LoadAssest(PathUtil.GetSoundPath(assetsName), action);

    /// <summary>
    /// 加载特效
    /// </summary>
    /// <param name="assetsName">资源名</param>
    /// <param name="action">回调</param>
    public void LoadEffect(string assetsName, Action<UnityEngine.Object> action = null) => LoadAssest(PathUtil.GetEffectPath(assetsName), action);

    /// <summary>
    /// 加载场景
    /// </summary>
    /// <param name="assetsName">资源名</param>
    /// <param name="action">回调</param>
    public void LoadScone(string assetsName, Action<UnityEngine.Object> action = null) => LoadAssest(PathUtil.GetScenePath(assetsName), action);

    /// <summary>
    /// 加载Lua
    /// </summary>
    /// <param name="assetsName">资源名</param>
    /// <param name="action">回调</param>
    public void LoadLua(string assetsName, Action<UnityEngine.Object> action = null) => LoadAssest(assetsName, action);

    /// <summary>
    /// 加载模型
    /// </summary>
    /// <param name="assetsName">资源名</param>
    /// <param name="action">回调</param>
    public void LoadPrefab(string assetsName, Action<UnityEngine.Object> action = null) => LoadAssest(assetsName, action);
    #endregion

}
