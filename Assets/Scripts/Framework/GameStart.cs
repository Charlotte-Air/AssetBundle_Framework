 using System.Collections;
using System.Collections.Generic;
using UnityEngine;

 public class GameStart : MonoBehaviour
{
    public GameMode GameMode;

    //void Start()
    //{
    //    Manager.Event.Subscribe(1,OnLuaInit);
    //    AppConst.gameMode = this.GameMode;
    //    DontDestroyOnLoad(this);
    //    Manager.Lua.Init();
    //    Manager.Resourece.ParseVersionFile();
    //}

    //void OnLuaInit(object args)
    //{
    //    Manager.Lua.StartLua("main");
    //    XLua.LuaFunction func = Manager.Lua.LuaEnv.Global.Get<XLua.LuaFunction>("Test");
    //    func.Call();
    //}

    //void OnApplicationQuit()
    //{
    //    Manager.Event.UnSubscribe(1,OnLuaInit);
    //}

}
