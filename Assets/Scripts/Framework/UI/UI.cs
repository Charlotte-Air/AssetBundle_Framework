using System;
using UnityEngine;

public class UI : LuaBehaviour
{
    Action LuaOnOpen;
    Action LuaOnClose;
    Action LuaCharacterInit;
    public string AssestName;

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
    
    public void OnOpen() => LuaOnOpen?.Invoke();
    
    public void OnClose()
    {
        LuaOnClose?.Invoke();
        GameManager.Instance.GetManager<ObjectPoolManager>(GameManager.ManagerName.Pool).RecycleObject(PoolType.UI, AssestName, this.gameObject);
    }
    
    protected override void Clear()
    {   //释放
        base.Clear();
        LuaOnOpen = null;
        LuaOnClose = null;
    }

    public void CharacterInit()
    {
        this.gameObject.SetActive(false);
        this.gameObject.SetActive(true);
    }
}
