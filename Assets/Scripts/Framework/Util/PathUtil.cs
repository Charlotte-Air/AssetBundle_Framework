using UnityEngine;

public static class PathUtil
{
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

    #endregion
}
