using UnityEngine;

 public class GameStart : MonoBehaviour
{
    public bool OpenLog;
    public GameMode GameMode;
    
    void Start()
    {
        Manager.Event.Subscribe((int)GameEvent.StartLua, StartLua);
        Manager.Event.Subscribe((int)GameEvent.GameInit, GameInit);
        AppConst.gameMode = this.GameMode;
        AppConst.OpenLog = this.OpenLog;
        DontDestroyOnLoad(this);
        if (AppConst.gameMode == GameMode.UpdateMode)
            this.gameObject.AddComponent<HotUpdate>();
        else
            Manager.Event.PerformEvent((int) GameEvent.GameInit);
    }
    
    void GameInit(object args)
    {
        if (AppConst.gameMode != GameMode.EditorMode)
            Manager.Resourece.ParseVersionFile();
        Manager.Lua.Init();
    }

    void StartLua(object args)
    {
        Manager.Pool.CreateGameObjectPool("UI", 10);
        Manager.Pool.CreateAssestPool("AssestBundle", 10);
        Manager.Lua.StartLua("main");
        XLua.LuaFunction func = Manager.Lua.LuaEnv.Global.Get<XLua.LuaFunction>("Test");
        func.Call();
    }

    void OnApplicationQuit()
    {
        Manager.Event.UnSubscribe((int)GameEvent.StartLua, StartLua);
        Manager.Event.UnSubscribe((int)GameEvent.GameInit, GameInit);
    }
}
