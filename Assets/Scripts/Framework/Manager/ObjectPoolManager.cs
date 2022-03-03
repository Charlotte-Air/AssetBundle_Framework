using UnityEngine;
using System.Collections.Generic;
public class ObjectPoolManager : MonoBehaviour
{
    Transform PoolParent; //父节点
    Dictionary<string, ObjectBase> Pools = new Dictionary<string, ObjectBase>(); //对象池

    void Awake()
    {
        PoolParent = this.transform.parent.Find("Pool");
    }

    /// <summary>
    /// 创建对象池
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="PoolName">对象池名</param>
    /// <param name="releaseTime">释放时间</param>
    private void CreatePool<T>(string PoolName, float releaseTime) where T : ObjectBase
    {
        if (!this.Pools.TryGetValue(name, out ObjectBase pool))
        {
            GameObject go = new GameObject(PoolName);
            go.transform.SetParent(this.PoolParent);
            pool = go.AddComponent<T>();
            pool.Init(releaseTime);
            this.Pools.Add(PoolName, pool);
        }
    }
    
    #region 公共接口
    /// <summary>
    /// 创建物体对象池
    /// </summary>
    /// <param name="poolName">对象池名称</param>
    /// <param name="releaseTime">释放时间</param>
    public void CreateGameObjectPool(string poolName, float releaseTime) => CreatePool<ObjectPool>(poolName, releaseTime);

    /// <summary>
    /// 创建资源对象池
    /// </summary>
    /// <param name="poolName">对象池名称</param>
    /// <param name="releaseTime">释放时间</param>
    public void CreateAssestPool(string poolName, float releaseTime) => CreatePool<AssestPool>(poolName, releaseTime);

    /// <summary>
    /// 取出对象
    /// </summary>
    /// <param name="poolName">对象池名称</param>
    /// <param name="assestName">资源名</param>
    /// <returns></returns>
    public Object TakeObject(string poolName, string assestName)
    {
        if (this.Pools.TryGetValue(poolName, out ObjectBase pool))
        {
            return pool.Take(assestName);
        }
        return null;
    }

    /// <summary>
    /// 回收对象
    /// </summary>
    /// <param name="poolName">对象池名称</param>
    /// <param name="assestName">资源名</param>
    /// <param name="asset">对象</param>
    public void RecycleObject(string poolName, string assestName, Object asset)
    {
        if (this.Pools.TryGetValue(poolName, out ObjectBase pool))
        {
            pool.Recycle(assestName,asset);
        }
    }
    #endregion
}
