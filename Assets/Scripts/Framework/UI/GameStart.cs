using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStart : MonoBehaviour
{
    public GameMode GameMode;

    void Start()
    {
        AppConst.gameMode = this.GameMode;
        DontDestroyOnLoad(this);

        Manager.Resourece.ParseVersionFile();
        Manager.Lua.Init((() =>     //异步加载 ->同步使用
            {
                Manager.Lua.StartLua("main");
            })
            );

        /* 另一种方式调用Lua
        XLua.LuaFunction func = Manager.Lua.LuaEnv.Global.Get<XLua.LuaFunction>("Test");
        func.Call();
        */
    }
}
