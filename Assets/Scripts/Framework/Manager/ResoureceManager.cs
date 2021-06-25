using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UObject = UnityEngine.Object;

public class ResoureceManager : MonoBehaviour
{
    /// <summary>
    /// Bundle信息集合
    /// </summary>
    private Dictionary<string, BundleInfo> m_BundleInfos = new Dictionary<string, BundleInfo>();
    internal class BundleInfo
    {
        public string AssetsName;
        public string BundleName;
        public List<string> Dependences;
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
            m_BundleInfos.Add(bundeInfo.AssetsName,bundeInfo);

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
    /// <param name="action">完成回调</param>
    IEnumerator LoadBundleAsync(string assetsName,Action<UObject> action = null)
    {
        string bundleName = m_BundleInfos[assetsName].BundleName;
        string bundlePath = Path.Combine(PathUtil.BundleResourcePath, bundleName);
        List<string> dependences = m_BundleInfos[assetsName].Dependences;
        if (dependences != null && dependences.Count > 0)
        {
            for (int i = 0; i < dependences.Count; i++)
            {
                yield return LoadBundleAsync(dependences[i]);  //递归调用
            }
        }

        AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(bundlePath); //加载资源AB包
        yield return request;

        AssetBundleRequest bundleRequest = request.assetBundle.LoadAssetAsync(assetsName); //加载资源
        yield return bundleRequest;
        Debug.Log("->PackgeBundleLoadAssest");
        if (action != null && bundleRequest != null)
        {
            action.Invoke(bundleRequest.asset);
        }
    }
    
#if UNITY_EDITOR
    /// <summary>
    /// 编译器环境加载资源
    /// </summary>
    /// <param name="assestName">资源名</param>
    /// <param name="action"></param>
    public void EditorLoadAssest(string assestName, Action<UObject> action = null)
    {
        Debug.Log("->EditorLoadAssest");
        UObject obj = UnityEditor.AssetDatabase.LoadAssetAtPath(assestName, typeof(UObject));
        if (obj == null)
            Debug.LogError("Assest Name is Not Exist:" + assestName);
        action?.Invoke(obj); //回调
    }
#endif


    /// <summary>
    /// 加载资源
    /// </summary>
    private void LoadAssest(string assestName, Action<UObject> action)
    {
#if UNITY_EDITOR //避免Build出错
        if (AppConst.gameMode == GameMode.EditorMode)
            EditorLoadAssest(assestName, action);
#endif
        if (AppConst.gameMode != GameMode.EditorMode)
            StartCoroutine(LoadBundleAsync(assestName, action));
    }


    #region 加载接口

    /// <summary>
    /// 加载UI
    /// </summary>
    public void LoadUI(string assetsName, Action<UnityEngine.Object> action = null)
    {
        LoadAssest(PathUtil.GetUIPath(assetsName), action);
    }

    /// <summary>
    /// 加载音效
    /// </summary>
    public void LoadMusic(string assetsName, Action<UnityEngine.Object> action = null)
    {
        LoadAssest(PathUtil.GetMusicPath(assetsName), action);
    }

    /// <summary>
    /// 加载音乐
    /// </summary>
    public void LoadSound(string assetsName, Action<UnityEngine.Object> action = null)
    {
        LoadAssest(PathUtil.GetSoundPath(assetsName), action);
    }

    /// <summary>
    /// 加载特效
    /// </summary>
    public void LoadEffect(string assetsName, Action<UnityEngine.Object> action = null)
    {
        LoadAssest(PathUtil.GetEffectPath(assetsName), action);
    }

    /// <summary>
    /// 加载场景
    /// </summary>
    public void LoadScone(string assetsName, Action<UnityEngine.Object> action = null)
    {
        LoadAssest(PathUtil.GetScenePath(assetsName), action);
    }

    /// <summary>
    /// 加载Lua
    /// </summary>
    /// <param name="assetsName"></param>
    /// <param name="action"></param>
    public void LoadLua(string assetsName, Action<UnityEngine.Object> action = null)
    {
        LoadAssest(assetsName, action);
    }
    #endregion

}
