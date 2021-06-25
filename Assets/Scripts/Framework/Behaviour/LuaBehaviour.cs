using System;
using UnityEngine;
using XLua;

public class LuaBehaviour : MonoBehaviour
{
    public string LuaName;
    protected LuaTable ScriptEvn;
    private  LuaEnv LuaEvn = Manager.Lua.LuaEnv;
    private  Action  LuaInit;
    private  Action  LuaUpdate;
    private  Action  LuaOnDestroy;

    void Awake()
    {
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
        LuaEvn.DoString(Manager.Lua.GetLuaScripts(luaName), luaName, ScriptEvn);
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
        LuaOnDestroy?.Invoke();
        Clear();
    }


    void OnApplicationQuit()
    {
        Clear();
    }

    protected virtual void Clear()
    {   //释放
        LuaOnDestroy = null;
        ScriptEvn?.Dispose();
        ScriptEvn = null;
        LuaInit = null;
        LuaUpdate = null;
    }

}
