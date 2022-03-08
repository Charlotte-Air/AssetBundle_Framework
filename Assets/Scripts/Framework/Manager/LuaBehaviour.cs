using XLua;
using System;
using UnityEngine;

public class LuaBehaviour : MonoBehaviour
{
    LuaEnv LuaEvn;
    Action  LuaInit;
    Action  LuaUpdate;
    Action  LuaOnDestroy;
    public string LuaName;
    protected LuaTable ScriptEvn;
    
    void Awake()
    {
        LuaEvn = GameManager.Instance.GetManager<LuaManager>(GameManager.ManagerName.Lua).LuaEnv;
        ScriptEvn = LuaEvn.NewTable();
        //设置元表
        LuaTable meat = LuaEvn.NewTable();
        meat.Set("__index", LuaEvn.Global);
        ScriptEvn.SetMetaTable(meat);
        meat.Dispose();
        ScriptEvn.Set("self", this);
    }

    public virtual void Init(string luaName)
    {
        //获取方法
        LuaEvn.DoString(GameManager.Instance.GetManager<LuaManager>(GameManager.ManagerName.Lua).GetLuaScripts(luaName), luaName, ScriptEvn);
        ScriptEvn.Get("OnInit", out LuaInit);
        ScriptEvn.Get("Update", out LuaUpdate);
        LuaInit?.Invoke();
    }

    void Update()
    {
        LuaUpdate?.Invoke();
    }

    void OnDestroy()
    {
        if (LuaOnDestroy != null)
        {
            LuaOnDestroy.Invoke();
            Clear();
        }
    }
    
    void OnApplicationQuit()
    {
        Clear();
    }

    protected virtual void Clear()
    {
        LuaOnDestroy = null;
        ScriptEvn?.Dispose();
        ScriptEvn = null;
        LuaInit = null;
        LuaUpdate = null;
    }
}
