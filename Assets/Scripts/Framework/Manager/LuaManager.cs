using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using XLua;

public class LuaManager : MonoBehaviour
{
    /// <summary>
    ///  Lua文件名列表
    /// </summary>
    public List<string> LuaNames = new List<string>();
    /// <summary>
    /// 缓存Lua脚本集合
    /// </summary>
    public Dictionary<string, byte[]> LuaScripts = new Dictionary<string, byte[]>();
    /// <summary>
    /// Lua虚拟机
    /// </summary>
    public LuaEnv LuaEnv;
    /// <summary>
    ///  Lua加载器
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    byte[] Loader(ref string name)
    {
        return GetLuaScripts(name);
    }

    void Update()
    {
        if (LuaEnv != null)
        {
            LuaEnv.Tick();
        }
    }
    
    void OnDestroy()
    {
        if(LuaEnv != null)
        {
            LuaEnv.Dispose();
            LuaEnv = null;
        }
    }

    /// <summary>
    /// 初始化
    /// </summary>
    public void Init()
    {
        LuaEnv = new LuaEnv();
        LuaEnv.AddLoader(Loader);

#if UNITY_EDITOR //避免Build出错
        if (AppConst.gameMode == GameMode.EditorMode)
            EditorLoadLuaScripts();
#endif
        if (AppConst.gameMode != GameMode.EditorMode)
            LoadLuaScripts();
    }

    /// <summary>
    /// 添加Lua脚本
    /// </summary>
    /// <param name="assetsName">资源名</param>
    /// <param name="luascipts"></param>
    public void AddLuaScripts(string assetsName, byte[] luascipts)
    {
        LuaScripts[assetsName] = luascipts;
    }

    /// <summary>
    /// 获取Lua脚本
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public byte[] GetLuaScripts(string name)
    {
        name = name.Replace(".", "/");
        string fileName = PathUtil.GetLuaPath(name);
        byte[] luaScripts = null;
        if (!LuaScripts.TryGetValue(fileName, out luaScripts))
            Debug.LogError("Lua Scripts is Not Exist");
        return luaScripts;
    }

    /// <summary>
    /// 加载Lua脚本
    /// </summary>
    void LoadLuaScripts()
    {
        foreach (string name in LuaNames)
        {
            Manager.Resourece.LoadLua(name, (UnityEngine.Object obj) =>
            {
                AddLuaScripts(name, (obj as TextAsset).bytes);
                if (LuaScripts.Count >= LuaNames.Count)
                {   //所有Lua加载完成时
                    Manager.Event.PerformEvent(1);
                    LuaNames.Clear();
                    LuaNames = null;
                }
            });
        }
    }

    /// <summary>
    /// 执行Lua脚本
    /// </summary>
    /// <param name="name"></param>
    public void StartLua(string name)
    {
        LuaEnv.DoString(string.Format("require '{0}'", name));
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编译器模式加载Lua脚本
    /// </summary>
    void EditorLoadLuaScripts()
    {
        string[] luaFiles = Directory.GetFiles(PathUtil.LuaPath, "*.bytes", SearchOption.AllDirectories); //搜索Lua文件夹
        for (int i = 0; i < luaFiles.Length; i++)
        {
            string fileName = PathUtil.GetStandardPath(luaFiles[i]);
            byte[] file = File.ReadAllBytes(fileName);
            AddLuaScripts(PathUtil.GetUnityPath(fileName), file);
        }
        Manager.Event.PerformEvent(1);
    }
#endif

}


