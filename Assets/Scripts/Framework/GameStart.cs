  using System.Collections;
using System.Collections.Generic;
using UnityEngine;

 public class GameStart : MonoBehaviour
{
    public GameMode GameMode;
    public bool OpenLog;

    void Start()
    {
        Manager.Event.Subscribe(1, OnLuaInit);
        AppConst.gameMode = this.GameMode;
        AppConst.OpenLog = this.OpenLog;
        DontDestroyOnLoad(this);
        Manager.Pool.CreateGameObjectPool("UI", 10);
        Manager.Pool.CreateAssestPool("AssestBundle", 10);
        Manager.Resourece.ParseVersionFile();
        Manager.Lua.Init();
    }

    void OnLuaInit(object args)
    {
        Manager.Lua.StartLua("main");
        XLua.LuaFunction func = Manager.Lua.LuaEnv.Global.Get<XLua.LuaFunction>("Test");
        func.Call();
    }

    void OnApplicationQuit()
    {
        Manager.Event.UnSubscribe(1, OnLuaInit);
    }

}
