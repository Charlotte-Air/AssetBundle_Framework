using UnityEngine;
using System.Collections.Generic;

public class ObjectInfo
{
    public UnityEngine.Object Object; //具体对象
    public string Name; //对象名称
    public System.DateTime LastUserTime; //销毁时间
    public ObjectInfo(string name,UnityEngine.Object obj)
    {
        Name = name;
        Object = obj;
        LastUserTime = System.DateTime.Now;
    }
}

public class ObjectBase : MonoBehaviour
{
    /// <summary>
    /// 自动释放时间
    /// (单位/秒)
    /// </summary>
    protected float ReleaseTime;

    /// <summary>
    /// 上次释放资源时间
    /// (单位/毫微秒)10000000 =1/秒
    /// </summary>
    protected long LastReleaseTime = 0;

    /// <summary>
    /// 对象池
    /// </summary>
    protected List<ObjectInfo> objectPools;

    public void Start()
    {
        LastReleaseTime = System.DateTime.Now.Ticks;
    }

    void Update()
    {
        if (System.DateTime.Now.Ticks - this.LastReleaseTime >= ReleaseTime * 10000000)
        {
            LastReleaseTime = System.DateTime.Now.Ticks;
            Release();
        }
    }
    
    public void Init(float time)
    {
        ReleaseTime = time;
        objectPools = new List<ObjectInfo>();
    }

    /// <summary>
    /// 取出对象
    /// </summary>
    /// <param name="name">名称</param>
    /// <returns></returns>
    public virtual Object Take(string name)
    {
        var pool = objectPools.GetEnumerator();
        while (pool.MoveNext())
        {
            if (pool.Current.Name == name)
            {
                objectPools.Remove(pool.Current);
                return pool.Current.Object;
            }
        }
        return null;
    }

    /// <summary>
    /// 回收对象
    /// </summary>
    /// <param name="name">名称</param>
    /// <param name="obj">对象</param>
    public virtual void Recycle(string name, UnityEngine.Object obj) => objectPools.Add(new ObjectInfo(name, obj));

    /// <summary>
    /// 释放对象
    /// </summary>
    public virtual void Release() { }
}
