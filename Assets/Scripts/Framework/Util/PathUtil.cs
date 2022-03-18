using System;
using System.IO;
using Charlotte;
using UnityEngine;

public static class PathUtil
{
    //***********************************************************************************************************************
    ///<summary>
    /// Assets根目录
    /// </summary>
    public static readonly string AssetsPath = Application.dataPath;
    ///<summary>
    /// Build输入目录
    /// </summary>
    public static readonly string BuildResourcesPath = AssetsPath + "/AssetBundle/";
    ///<summary>
    /// Build输出目录
    /// </summary>
    public static readonly string BuildeOutPath = Application.streamingAssetsPath;
    ///<summary>
    /// Build资源路径
    /// </summary>
    public static string BundleResourcePath
    {
        get
        {
            if (AppConst.gameMode == AppConst.GameMode.UpdateMode)
                return ReadWritePath;
            return ReadPath;
        }
    }
    /// <summary>
    /// 只读目录
    /// </summary>
    public static readonly string ReadPath = Application.streamingAssetsPath;
    /// <summary>
    /// 可读写目录
    /// </summary>
    public static readonly string ReadWritePath = Application.persistentDataPath;
    /// <summary>
    /// Lua目录路径
    /// </summary>
    public static readonly string LuaPath = "Assets/AssetBundle/LuaScripts";
    /// <summary>
    /// 获取Unity相对路径
    /// </summary>
    /// <param name="path">路径</param>
    /// <returns></returns>
    public static string GetUnityPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;
        return path.Substring(path.IndexOf("Assets"));
    }
    /// <summary>
    /// 获取标准路径
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string GetStandardPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;
        return path.Trim().Replace("\\", "/");
    }
    
    //***********************************************************************************************************************
    public static string MOD_NAME = "mod_default";
    public static string UI_MOD_NAME = "mod_default/UI";
    /// <summary>
    /// 版本控制文件路径
    /// </summary>
    public static string VesionConfig = "/Vesion/config.txt";
    /// <summary>
    /// AssetBundlePath路径
    /// </summary>
    public static string AssetBundlePath = "Assets/AssetBundle/";
    public static string AssetBundleFileName = ".assetbundle";
    /// <summary>
    /// AssetBundlePath HTTP路径
    /// </summary>
    public static string AssetBundleUrlPath = "file:///" + Application.dataPath + "/streamingassets/" +
#if UNITY_ANDROID
                                              "android/mod_default/";
#elif UNITY_STANDALONE
     "standalonewindows/mod_default/";
#endif
    
    /// <summary>
    /// AssetBundlePathHttp地址获取
    /// </summary>
    public static string AssetBundleHttp { get { return AssetBundleUrlPath; } 
        set 
        { 
            AssetBundleUrlPath = string.IsNullOrEmpty(value) ? ("file:///" + Application.dataPath + "/streamingassets/" +
#if UNITY_ANDROID
                                                                "android" +
#elif UNITY_STANDALONE
            "standalonewindows" +
#endif
                                                                "/mod_default/") : value;
            if (AssetBundleUrlPath.StartsWith("http://") && (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer))
                isUrlEncode = true;
            else
                isUrlEncode = false;
        } 
    }
    static bool isUrlEncode = false;
    public static bool IsUrlEncode { get { return isUrlEncode; } }
    //***********************************************************************************************************************


    #region 资源接口
    
    ///<summary>
    /// 获取Lua路径
    /// </summary>
    public static string GetLuaPath(string name) => $"Assets/AssetBundle/LuaScripts/{name}.bytes";

    ///<summary>
    /// 获取UI路径
    /// </summary>
    public static string GetUIPath(string name) => $"Assets/AssetBundle/UI/prefabs/{name}.prefab";

    ///<summary>
    /// 获取场景路径
    /// </summary>
    public static string GetScenePath(string name) => $"Assets/AssetBundle/Scenes/{name}.unity";

    ///<summary>
    /// 获取特效路径
    /// </summary>
    public static string GetEffectPath(string name) => $"Assets/AssetBundle/Effect/prefabs/{name}.prefab";

    ///<summary>
    /// 获取模型路径
    /// </summary>
    public static string GetModelPath(string name) => $"Assets/AssetBundle/Model/prefabs/{name}.prefab";

    ///<summary>
    /// 获取音乐路径
    /// </summary>
    public static string GetMusicPath(string name) => $"Assets/AssetBundle/Audio/Music/{name}";

    ///<summary>
    /// 获取音效路径
    /// </summary>
    public static string GetSoundPath(string name) => $"Assets/AssetBundle/Audio/Sound/{name}";

    ///<summary>
    /// 获取贴图路径
    /// </summary>
    public static string GetSpritePath(string name) => $"Assets/AssetBundle/Sprite/{name}";
    
    //**************************************************************************************************************//
    /// <summary>
    /// 获取AssetBundle全路径
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string GetAssetBundleFullPath(string path) 
    {
        switch (GetAssetBundleType()) 
        {
            case AssetBundlePack.PackType.WebPlayer:
                return AssetBundleUrlPath + AssetBundlePack.GetName(AssetBundlePack.PackType.WebPlayer) + "/" + path;
            case AssetBundlePack.PackType.iOS:
                return "file://" + Application.streamingAssetsPath + "/" + AssetBundlePack.GetName(AssetBundlePack.PackType.iOS) + "/" + path;
            case AssetBundlePack.PackType.Android:
#if UNITY_ANDROID
                if (AppConst.IsUsePatch)
                {
                    if (path.IndexOf('.') != -1)
                    {
                        string patchFilePath = string.Format("{0}/patch/{1}/{2}", Application.persistentDataPath, "android/mod_default/", path);
                        if (File.Exists(patchFilePath))
                        {
                            return "file://" + patchFilePath;
                        }
                    }
                    else if (path == "")
                    {
                        string patchDirectoryPath = string.Format("{0}/patch/{1}/{2}", Application.persistentDataPath, "android/mod_default/", path);
                        if (Directory.Exists(patchDirectoryPath))
                        {
                            return "file://" + patchDirectoryPath;
                        }
                    }
                }
#endif
                return Application.streamingAssetsPath + "/android/mod_default/" + path;
            case AssetBundlePack.PackType.Windows:
                return AssetBundleUrlPath + path;
            default:
                return Application.streamingAssetsPath + "/" + AssetBundlePack.GetName(Application.platform.ToString()) + "/" + path;
        }
    }
    
    /// <summary>
    /// 获取AssetBundle平台类型
    /// </summary>
    /// <returns></returns>
    public static AssetBundlePack.PackType GetAssetBundleType()
    {
#if UNITY_WEBPLAYER
            return AssetBundlePack.PackType.WebPlayer;
#elif (UNITY_IOS || UNITY_IPHONE)
            return AssetBundlePack.PackType.iOS;
#elif UNITY_ANDROID
        return AssetBundlePack.PackType.Android;
#elif UNITY_STANDALONE
            return AssetBundlePack.PackType.Windows;
#else
            return AssetBundlePack.PackType.None;
#endif
// #if UNITY_EDITOR
//             if (!Charlotte.ResourceManager) 
//             {
//                 return AssetBundlePack.PackType.WebPlayer; //编辑器模式下调试模拟下载,可以使用远程路径
//             }
// #endif
    }
    
    /// <summary>
    /// 版本文件本地文件，用于写入版本文件
    /// </summary>
    /// <param name="type"></param>
    /// <param name="filePath"></param>
    /// <param name="mustStreamingAssets"></param>
    /// <returns></returns>
    public static string GetVersionConfigPath(AssetBundlePack.PackType type, string filePath, bool mustStreamingAssets = false) 
    {
        if (mustStreamingAssets)
        {
            return Application.streamingAssetsPath + "/" + AssetBundlePack.GetName(type) + filePath;
        }
#if UNITY_ANDROID
        if (AppConst.IsUsePatch)
        {
            if (filePath.IndexOf('.') != -1)
            {
                string patchFilePath = string.Format("{0}/patch/{1}/{2}", Application.persistentDataPath, AssetBundlePack.GetName(type), filePath);
                if (File.Exists(patchFilePath))
                {
                    return "file://" + patchFilePath;
                }
            }
        }
#endif
        return Application.streamingAssetsPath + "/" + AssetBundlePack.GetName(type) + filePath;
    }
    
    /// <summary>
    /// AssetBundle效应
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns></returns>
    public static string FileToAssetBundleName(string fileName) 
    {
        try 
        {
            string name = Path.GetDirectoryName(fileName);
            name = PathToAssetBundleName(name);
            return name;
        } 
        catch (Exception e) 
        {
            Debug.LogError("文件路径[{0}]不存在 " + fileName + " " + e.ToString());
        }
        return PathToAssetBundleName(fileName);
    }
    
    /// <summary>
    /// AssetBundle效应
    /// </summary>
    /// <param name="path">路径</param>
    /// <returns></returns>
    public static string PathToAssetBundleName(string path) 
    {
        path = path.Replace('\\', '/');
        if (path.StartsWith(AssetBundlePath)) 
        {
            path = path.Substring(AssetBundlePath.Length);
        }
        return (path + AssetBundleFileName).ToLower();
    }
    
    /// <summary>
    /// 是否存在AssetBundle文件夹中
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static bool IsAssetBundleDir(string path) 
    {
        bool res = false;
#if UNITY_EDITOR
        if (path.StartsWith(AssetBundlePath))
            res = File.Exists(path);
        else
            res = File.Exists(AssetBundlePath + path);
#endif
        return res;
    }
    
    /// <summary>
    /// 判断是否UI资源
    /// </summary>
    /// <param name="assetName"></param>
    /// <returns></returns>
    public static bool IsUIAsset(string assetName)
    {
        string dirPath = Path.GetDirectoryName(assetName).Replace('\\', '/');
        if (assetName.Contains("ui/"))
        {
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 判断是否通用资源
    /// </summary>
    /// <param name="assetName"></param>
    /// <returns></returns>
    public static bool IsCommonAsset(string assetName)
    {
        string dirPath = Path.GetDirectoryName(assetName).Replace('\\', '/');
        if (assetName.Contains("json/") || assetName.Contains("data/"))
        {
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 判断是否战斗资源
    /// </summary>
    /// <param name="assetName"></param>
    /// <returns></returns>
    public static bool ISBattleAsset(string assetName)
    {
        string dirPath = Path.GetDirectoryName(assetName).Replace('\\', '/');
        if (assetName.Contains("Battle/"))
        {
            return true;
        }
        return false;
    }
    
    #endregion
}
