using System;
using UnityEngine;

public class Scene : LuaBehaviour
{
    public string SceneName;
    private Action LuaActive;
    private Action LuaInActive;
    private Action LuaOnEnter;
    private Action LuaOnQuit;

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="luaName">Lua脚本名</param>
    public override void Init(string luaName)
    {
        base.Init(luaName);

        ScriptEvn.Get("OnActive", out LuaActive);
        ScriptEvn.Get("OnInActive", out LuaInActive);
        ScriptEvn.Get("OnEnter", out LuaOnEnter);
        ScriptEvn.Get("OnQuit", out LuaOnQuit);
    }

    /// <summary>
    /// 激活
    /// </summary>
    public void OnActive()
    {
        LuaActive?.Invoke();
    }

    /// <summary>
    /// 非激活
    /// </summary>
    public void OnInActive()
    {
        LuaInActive?.Invoke();
    }

    /// <summary>
    /// 进入
    /// </summary>
    public void OnEnter()
    {
        LuaOnEnter?.Invoke();
    }

    /// <summary>
    /// 离开
    /// </summary>
    public void OnQuit()
    {
        LuaOnQuit?.Invoke();
    }

    /// <summary>
    /// 清空
    /// </summary>
    protected override void Clear()
    {   //释放
        base.Clear();
        LuaActive = null;
        LuaInActive = null;
        LuaOnEnter = null;
        LuaOnQuit = null;
    }
}
