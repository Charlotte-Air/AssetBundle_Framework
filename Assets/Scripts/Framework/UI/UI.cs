using System;
using UnityEngine;

public class UILogic : LuaBehaviour
{
    private Action LuaOnOpen;
    private Action LuaOnClose;

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="luaName">Lua脚本名</param>
    public override void Init(string luaName)
    {
        base.Init(luaName);
        ScriptEvn.Get("OnOpen", out LuaOnOpen);
        ScriptEvn.Get("OnClose", out LuaOnClose);
    }

    /// <summary>
    /// 打开
    /// </summary>
    public void OnOpen()
    {
        LuaOnOpen?.Invoke();
    }

    /// <summary>
    /// 关闭
    /// </summary>
    public void OnClose()
    {
        LuaOnClose?.Invoke();
    }

    /// <summary>
    /// 清空
    /// </summary>
    protected override void Clear()
    {   //释放
        base.Clear();
        LuaOnOpen = null;
        LuaOnClose = null;
    }
}
