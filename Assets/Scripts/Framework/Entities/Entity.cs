using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity : LuaBehaviour
{
    private Action LuaOnShow;
    private Action LuaOnHide;

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="luaName">Lua脚本名</param>
    public override void Init(string luaName)
    {
        base.Init(luaName);
        ScriptEvn.Get("OnShow", out LuaOnShow);
        ScriptEvn.Get("OnHide", out LuaOnHide);
    }

    /// <summary>
    /// 显示
    /// </summary>
    public void OnShow()
    {
        LuaOnShow?.Invoke();
    }

    /// <summary>
    /// 隐藏
    /// </summary>
    public void OnHide()
    {
        LuaOnHide?.Invoke();
    }

    /// <summary>
    /// 清空
    /// </summary>
    protected override void Clear()
    {   //释放
        base.Clear();
        LuaOnShow = null;
        LuaOnHide = null;
    }

}
