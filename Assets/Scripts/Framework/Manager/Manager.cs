using UnityEngine;

public class Manager : MonoBehaviour
{
    private static ResoureceManager _resource;
    public static ResoureceManager Resourece { get { return _resource; } }

    private static LuaManager _lua;
    public static LuaManager Lua { get { return _lua; } }

    private static UIManager  _ui;
    public static UIManager UI { get { return _ui; } }

    private void Awake()
    {
        _resource = this.gameObject.AddComponent<ResoureceManager>();
        _lua = this.gameObject.AddComponent<LuaManager>();
        _ui = this.gameObject.AddComponent<UIManager>();
    }

}
