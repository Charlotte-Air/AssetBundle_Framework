using UnityEngine;

 public class GameStart : MonoBehaviour
{
    public bool OpenLog;
    public AppConst.GameMode GameMode;

    void Awake()
    {
        GameManager.Instance.Init();
        AppConst.gameMode = this.GameMode;
        AppConst.OpenLog = this.OpenLog;
    }

    void Start()
    {
        GameManager.Instance.GetManager<NetManager>(GameManager.ManagerName.Net).Init();
        var message = GameManager.Instance.GetManager<MessageManager>(GameManager.ManagerName.Message);
        message.Subscribe(MessageType.GameInit, GameInit);
        message.Subscribe(MessageType.StartLua, StartLua);
        DontDestroyOnLoad(this);
        if (AppConst.gameMode == AppConst.GameMode.UpdateMode)
            gameObject.AddComponent<HotUpdate>();
        else
            message.NotifyMessage(MessageType.GameInit);
    }
    
    void GameInit(object o)
    {
        if (AppConst.gameMode != AppConst.GameMode.EditorMode)
        {
            GameManager.Instance.GetManager<ResoureceManager>(GameManager.ManagerName.Resourece).ParseVersionFile();
        }
        GameManager.Instance.GetManager<LuaManager>(GameManager.ManagerName.Lua).Init();
        GameManager.Instance.GetManager<ObjectPoolManager>(GameManager.ManagerName.Pool).Init();
    }

    void StartLua(object o)
    {
        var lua = GameManager.Instance.GetManager<LuaManager>(GameManager.ManagerName.Lua);
        lua.StartLua("Main"); 
        lua.LuaEnv.Global.Get<XLua.LuaFunction>("MainInit").Call();
    }

    void OnApplicationQuit()
    {
        var message = GameManager.Instance.GetManager<MessageManager>(GameManager.ManagerName.Message);
        message.Unsubscribe(MessageType.GameInit);
        message.Unsubscribe(MessageType.StartLua);
    }
}
