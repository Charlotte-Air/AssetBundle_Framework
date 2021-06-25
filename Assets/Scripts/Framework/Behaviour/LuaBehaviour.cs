using System;
using UnityEngine;
using XLua;

public class LuaBehaviour : MonoBehaviour
{
    public string LuaName;
    protected LuaTable m_ScriptEvn;
    private  LuaEnv m_LuaEvn = Manager.Lua.LuaEnv;
    private  Action  m_LuaInit;
    private  Action  m_LuaUpdate;
    private  Action  m_LuaOnDestroy;

    void Awake()
    {
        m_ScriptEvn = m_LuaEvn.NewTable();
        //设置元表
        LuaTable meat = m_LuaEvn.NewTable();
        meat.Set("__index", m_LuaEvn.Global);
        m_ScriptEvn.SetMetaTable(meat);
        meat.Dispose();
        m_ScriptEvn.Set("self", this);
    }

    public virtual void Init(string luaName)
    {
        //获取方法
        m_LuaEvn.DoString(Manager.Lua.GetLuaScripts(luaName), luaName, m_ScriptEvn);
        m_ScriptEvn.Get("OnInit", out m_LuaInit);
        m_ScriptEvn.Get("Update", out m_LuaUpdate);
        m_LuaInit?.Invoke();
    }

    void Update()
    {
        m_LuaUpdate?.Invoke();
    }

    void OnDestroy()
    {
        m_LuaOnDestroy?.Invoke();
        Clear();
    }


    void OnApplicationQuit()
    {
        Clear();
    }

    protected virtual void Clear()
    {   //释放
        m_LuaOnDestroy = null;
        m_ScriptEvn?.Dispose();
        m_ScriptEvn = null;
        m_LuaInit = null;
        m_LuaUpdate = null;
    }

}
