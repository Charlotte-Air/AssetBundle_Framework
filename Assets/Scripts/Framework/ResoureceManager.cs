using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Uobject = UnityEngine.Object;

public class ResoureceManager : MonoBehaviour
{
    /// <summary> Bundle信息集合 </summary>
    private Dictionary<string, BundleInfo> m_BundleInfos = new Dictionary<string, BundleInfo>();
    internal class BundleInfo
    {
        public string AssetsName;
        public string BundleName;
        public List<string> Dependences;
    }

    void Start()
    {
        ParseVersionFile();
        LoadUI("Login/Character", OnInstantiate);
    }

    /// <summary>
    /// 解析版本文件
    /// </summary>
    private void ParseVersionFile()
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
        }
    }

    /// <summary>
    /// 异步加载资源
    /// </summary>
    /// <param name="assetsName">资源名</param>
    /// <param name="action">完成回调</param>
    IEnumerator LoadBundleAsync(string assetsName,Action<Uobject> action = null)
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

    /// <summary> 回调 </summary>
    private void OnInstantiate(Uobject obj)
    {
        GameObject go = Instantiate(obj) as GameObject;
        go.transform.SetParent(this.transform);
        go.SetActive(true);
        go.transform.localPosition = Vector3.zero;
    }

    /// <summary>
    /// 编译器环境加载资源
    /// </summary>
    /// <param name="assestName">资源名</param>
    /// <param name="action"></param>
    void EditorLoadAssest(string assestName, Action<Uobject> action = null)
    {
        Debug.Log("->EditorLoadAssest");
        Uobject obj = UnityEditor.AssetDatabase.LoadAssetAtPath(assestName, typeof(Uobject));
        if (obj == null)
        {
            Debug.LogError("Assest Name is Not Exist:" + assestName);
        }
        action?.Invoke(obj); //回调
    }

    /// <summary> 资源加载接口 </summary>
    private void LoadAssest(string assestName, Action<Uobject> action)
    {
        if (AppConst.gameMode == GameMode.EditorMode)
            EditorLoadAssest(assestName, action);
        else
            StartCoroutine(LoadBundleAsync(assestName, action));
    }

    /// <summary>加载UI</summary>
    public void LoadUI(string assetsName, Action<UnityEngine.Object> action = null)
    {
        LoadAssest(PathUtil.GetUIPath(assetsName), action);
    }

    /// <summary>加载音效</summary>
    public void LoadMusic(string assetsName, Action<UnityEngine.Object> action = null)
    {
        LoadAssest(PathUtil.GetMusicPath(assetsName), action);
    }

    /// <summary>加载音乐</summary>
    public void LoadSound(string assetsName, Action<UnityEngine.Object> action = null)
    {
        LoadAssest(PathUtil.GetSoundPath(assetsName), action);
    }

    /// <summary>加载特效</summary>
    public void LoadEffect(string assetsName, Action<UnityEngine.Object> action = null)
    {
        LoadAssest(PathUtil.GetEffectPath(assetsName), action);
    }

    /// <summary>加载场景</summary>
    public void LoadScone(string assetsName, Action<UnityEngine.Object> action = null)
    {
        LoadAssest(PathUtil.GetScenePath(assetsName), action);
    }
}
