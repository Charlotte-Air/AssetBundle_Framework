using System;
using UnityEngine;

public class UILogic : LuaBehaviour
{
    private Action m_LuaOnOpen;
    private Action m_LuaOnClose;

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="luaName"></param>
    public override void Init(string luaName)
    {
        base.Init(luaName);
        m_ScriptEvn.Get("OnOpen", out m_LuaOnOpen);
        m_ScriptEvn.Get("OnClose", out m_LuaOnClose);
    }

    /// <summary>
    /// 打开
    /// </summary>
    public void OnOpen()
    {
        m_LuaOnOpen?.Invoke();
    }

    /// <summary>
    /// 关闭
    /// </summary>
    public void OnClose()
    {
        m_LuaOnClose?.Invoke();
    }

    /// <summary>
    /// 清空
    /// </summary>
    protected override void Clear()
    {   //释放
        base.Clear();
        m_LuaOnOpen = null;
        m_LuaOnClose = null;
    }
}
