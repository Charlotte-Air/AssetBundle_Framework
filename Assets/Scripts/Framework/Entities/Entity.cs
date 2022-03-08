using System;
public class Entity : LuaBehaviour
{
    private Action LuaOnShow;
    private Action LuaOnHide;
    public override void Init(string luaName) //脚本名
    {
        base.Init(luaName);
        ScriptEvn.Get("OnShow", out LuaOnShow);
        ScriptEvn.Get("OnHide", out LuaOnHide);
    }
    public void OnShow() => LuaOnShow?.Invoke();
    public void OnHide() => LuaOnHide?.Invoke();
    protected override void Clear() //释放
    {
        base.Clear();   
        LuaOnShow = null;
        LuaOnHide = null;
    }
}
