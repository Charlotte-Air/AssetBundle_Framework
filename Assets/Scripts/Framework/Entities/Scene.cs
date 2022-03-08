using System;
public class Scene : LuaBehaviour
{
    Action LuaActive;
    Action LuaInActive;
    Action LuaOnEnter;
    Action LuaOnQuit;
    public override void Init(string luaName) //Lua脚本名
    {
        base.Init(luaName);
        ScriptEvn.Get("OnActive", out LuaActive);
        ScriptEvn.Get("OnInActive", out LuaInActive);
        ScriptEvn.Get("OnEnter", out LuaOnEnter);
        ScriptEvn.Get("OnQuit", out LuaOnQuit);
    }
    public void OnActive() =>  LuaActive?.Invoke();
    public void OnInActive() => LuaInActive?.Invoke();
    public void OnEnter() => LuaOnEnter?.Invoke();
    public void OnQuit() => LuaOnQuit?.Invoke();
    protected override void Clear()
    {
        base.Clear();
        LuaActive = null;
        LuaInActive = null;
        LuaOnEnter = null;
        LuaOnQuit = null;
    }
}
