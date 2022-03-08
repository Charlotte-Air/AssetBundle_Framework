using XLua;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

public class LuaManager : MonoBehaviour
{
    public LuaEnv LuaEnv; //Lua虚拟机
    List<string> LuaNames = new List<string>(); //Lua文件名列表
    Dictionary<string, byte[]> LuaScripts = new Dictionary<string, byte[]>(); //缓存Lua脚本集合

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
        LuaEnv.AddBuildin("rapidjson", XLua.LuaDLL.Lua.LoadRapidJson);
        LuaEnv.AddLoader(Loader);
#if UNITY_EDITOR //避免Build出错
        if (AppConst.gameMode == AppConst.GameMode.EditorMode)
            EditorLoadLuaScripts();
#endif
        if (AppConst.gameMode != AppConst.GameMode.EditorMode)
            LoadLuaScripts();
    }

    byte[] Loader(ref string name) => GetLuaScripts(name);
    
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
    /// 获取Lua脚本z
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
            GameManager.Instance.GetManager<ResoureceManager>(GameManager.ManagerName.Resourece).LoadLua(name, (UnityEngine.Object obj) =>
            {
                AddLuaScripts(name, (obj as TextAsset).bytes);
                if (LuaScripts.Count >= LuaNames.Count)
                {
                    GameManager.Instance.GetManager<MessageManager>(GameManager.ManagerName.Message).NotifyMessage(MessageType.StartLua);//所有Lua加载完成时
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

    public void TestLua(string name)
    {
        Debug.Log($"测试{name}");
    }

    public void AddLuaScript(string file) => LuaNames.Add(file);

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
        GameManager.Instance.GetManager<MessageManager>(GameManager.ManagerName.Message).NotifyMessage(MessageType.StartLua);
    }
#endif
}


