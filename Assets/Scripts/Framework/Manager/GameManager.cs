using System;
using UnityEngine;
using System.Collections.Generic;

public class GameManager
{
    public class ManagerName 
    {
        public const string Game = "Manager";
        public const string UI = "UIManager";
        public const string Lua = "LuaManager";
        public const string Net = "NetManager";
        public const string Scene = "SceneManager";
        public const string Sound = "SoundManager";
        public const string Entity = "EntityManager";
        public const string Message = "MessageManager";
        public const string Pool = "ObjectPoolManager";
        public const string Resourece = "ResoureceManager";
    }
    
    static GameManager instance;
    static GameObject gameManager;
    static Dictionary<string, object> managers = new Dictionary<string, object>();
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
                instance = new GameManager();
            return instance;
        }
    }
    public GameObject RootNodeManager
    {
        get 
        {
            if (gameManager == null)
                gameManager = GameObject.Find("Manager");
            return gameManager;
        }
    }
    
    public void Init()
    {
        AddManager<UIManager>(ManagerName.UI);
        AddManager<LuaManager>(ManagerName.Lua);
        AddManager<NetManager>(ManagerName.Net);
        AddManager<SceneManager>(ManagerName.Scene);
        AddManager<SoundManager>(ManagerName.Sound);
        AddManager<EntityManager>(ManagerName.Entity);
        AddManager<MessageManager>(ManagerName.Message);
        AddManager<ObjectPoolManager>(ManagerName.Pool);
        AddManager<ResoureceManager>(ManagerName.Resourece);
    }

    /// <summary>
    /// 添加管理器挂载到节点下
    /// </summary>
    /// <param name="typeName"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T AddManager<T>(string typeName) where T : Component 
    {
        object result = null;
        managers.TryGetValue(typeName, out result);
        if (result != null) 
        {
            return (T)result;
        }
        Component c = RootNodeManager.AddComponent<T>();
        managers.Add(typeName, c);
        return default(T);
    }

    /// <summary>
    /// 添加管理器
    /// </summary>
    /// <param name="typeName"></param>
    /// <param name="obj"></param>
    public void AddManager(string typeName, object obj) 
    {
        if (!managers.ContainsKey(typeName))
        {
            managers.Add(typeName, obj);
        }
    }
    
    /// <summary>
    /// 删除管理器
    /// </summary>
    public void RemoveManager(string typeName) 
    {
        if (managers.ContainsKey(typeName))
        {
            object manager = null;
            managers.TryGetValue(typeName, out manager);
            Type type = manager.GetType();
            if (type.IsSubclassOf(typeof(MonoBehaviour))) 
            {
                GameObject.Destroy((Component)manager);
            }
            managers.Remove(typeName);
        }
    }
    
    /// <summary>
    /// 获取管理器
    /// </summary>
    /// <param name="typeName"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T GetManager<T>(string typeName) where T : class 
    {
        if (!managers.ContainsKey(typeName)) 
        {
            return default(T);
        }
        object manager = null;
        managers.TryGetValue(typeName, out manager);
        return (T)manager;
    }

    public object GetManagerObject(string typeName)
    {
        if (typeName != string.Empty && managers.ContainsKey(typeName))
        {
            var o = managers[typeName];
            return o;
        }
        return null;
    }
}
