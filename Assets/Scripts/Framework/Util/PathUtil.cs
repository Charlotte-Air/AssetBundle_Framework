using UnityEngine;

public class PathUtil
{
    ///<summary> Assets根目录 </summary>
    public static readonly string AssetsPath = Application.dataPath;
    ///<summary> Build输入目录 </summary>
    public static readonly string BuildResourcesPath = AssetsPath + "/BuilResources/";
    ///<summary> Build输出目录 </summary>
    public static readonly string BuildeOutPath = Application.streamingAssetsPath;
    ///<summary> Build资源路径 </summary>
    public static string BundleResourcePath {  get { return Application.streamingAssetsPath;  } }

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

    ///<summary> 获取Lua路径 </summary>
    public static string GetLuaPath(string name)
    {
        return string.Format("Assets/BuilResources/LuaScripts/{0}.bytes", name);
    }

    ///<summary> 获取UI路径 </summary>
    public static string GetUIPath(string name)
    {
        return string.Format("Assets/BuilResources/UI/prefabs/{0}.prefab", name);
    }

    ///<summary> 获取场景路径 </summary>
    public static string GetScenePath(string name)
    {
        return string.Format("Assets/BuilResources/Scenes/{0}.unity", name);
    }

    ///<summary> 获取特效路径 </summary>
    public static string GetEffectPath(string name)
    {
        return string.Format("Assets/BuilResources/Effect/prefabs/{0}.prefab}", name);
    }

    ///<summary> 获取音效路径 </summary>
    public static string GetMusicPath(string name)
    {
        return string.Format("Assets/BuilResources/Audio/Music/{0}", name);
    }

    ///<summary> 获取音乐路径 </summary>
    public static string GetSoundPath(string name)
    {
        return string.Format("Assets/BuilResources/Audio/Sound/{0}", name);
    }

    ///<summary> 获取贴图路径 </summary>
    public static string GetSpritePath(string name)
    {
        return string.Format("Assets/BuilResources/Sprite/{0}", name);
    }

}
