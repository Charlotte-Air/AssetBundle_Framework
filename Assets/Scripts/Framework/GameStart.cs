using UnityEngine;

 public class GameStart : MonoBehaviour
{
    public bool OpenLog;
    public GameMode GameMode = GameMode.Default;
    
    void Start()
    {
        Manager.Event.Subscribe((int)GameEvent.StartLua, StartLua);
        Manager.Event.Subscribe((int)GameEvent.GameInit, GameInit);
        if (GameMode == GameMode.Default)
        {
#if UNITY_EDITOR
            GameMode = GameMode.UpdateMode;
#elif !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IPHONE)
            GameMode = GameMode.UpdateMode;
#endif
        }
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
