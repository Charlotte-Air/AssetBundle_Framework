using UnityEngine;

public class Manager : MonoBehaviour
{
    private static ResoureceManager resource;
    public static ResoureceManager Resourece { get { return resource; } }

    private static LuaManager lua;
    public static LuaManager Lua { get { return lua; } }

    private static UIManager  ui;
    public static UIManager UI { get { return ui; } }

    private static EntityManager entity;
    public static EntityManager Entity { get { return entity; } }

    private static SceneManager scene;
    public static SceneManager Scene { get { return scene; } }

    private static SoundManager sound;

    public static SoundManager Sound { get { return sound; } set { sound = value; } }

    private static EventManager events;
    public static EventManager Event { get { return events; } set { events = value; } }

    private void Awake()
    {
        resource = this.gameObject.AddComponent<ResoureceManager>();
        lua = this.gameObject.AddComponent<LuaManager>();
        ui = this.gameObject.AddComponent<UIManager>();
        entity = this.gameObject.AddComponent<EntityManager>();
        scene = this.gameObject.AddComponent<SceneManager>();
        Sound = this.gameObject.AddComponent<SoundManager>();
        Event = this.gameObject.AddComponent<EventManager>();

    }

}
