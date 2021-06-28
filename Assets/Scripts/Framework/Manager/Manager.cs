using UnityEngine;

public class Manager : MonoBehaviour
{
    private static UIManager ui;
    public static UIManager UI { get { return ui; } }

    private static LuaManager lua;
    public static LuaManager Lua { get { return lua; } }

    private static EntityManager entity;
    public static EntityManager Entity { get { return entity;} }

    private static SceneManager scene;
    public static SceneManager Scene { get { return scene; } }

    private static ResoureceManager resource;
    public static ResoureceManager Resourece { get { return resource; } }

    private static EventManager events;
    public static EventManager Event { get { return events; } set { events = value;} }

    private static SoundManager sound;
    public static SoundManager Sound { get { return sound; } set { sound = value; } }

    private static ObjectPoolManager pool;
    public static ObjectPoolManager Pool { get { return pool; } set { pool = value; } }

    private static NetManager net;
    public static NetManager Net { get { return net; } set { net = value; } }

    private void Awake()
    {
        resource = this.gameObject.AddComponent<ResoureceManager>();
        lua = this.gameObject.AddComponent<LuaManager>();
        ui = this.gameObject.AddComponent<UIManager>();
        entity = this.gameObject.AddComponent<EntityManager>();
        scene = this.gameObject.AddComponent<SceneManager>();
        Sound = this.gameObject.AddComponent<SoundManager>();
        Event = this.gameObject.AddComponent<EventManager>();
        Pool = this.gameObject.AddComponent<ObjectPoolManager>();
        Net = this.gameObject.AddComponent<NetManager>();
    }

}
