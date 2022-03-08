using UnityEngine;
using System.Collections.Generic;


public enum PoolType
{
    UI,
    AssetsBundle,
}

public class ObjectPoolManager : MonoBehaviour
{
    Transform PoolParent; //父节点
    Dictionary<PoolType, ObjectBase> Pools; //对象池

    void Awake()
    {
        PoolParent = this.transform.parent.Find("Pool");
        Pools = new Dictionary<PoolType, ObjectBase>();
    }

    public void Init()
    {
        CreateUIPool(10);
        CreateAssetsPool(10);
    }

    /// <summary>
    /// 创建对象池
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="PoolName">对象池名</param>
    /// <param name="releaseTime">释放时间</param>
    private void CreatePool<T>(PoolType poolType, float releaseTime) where T : ObjectBase
    {
        if (!Pools.TryGetValue(poolType,out ObjectBase pool))
        {
            GameObject go = new GameObject(poolType.ToString());
            go.transform.SetParent(this.PoolParent);
            pool = go.AddComponent<T>();
            pool.Init(releaseTime);
            this.Pools.Add(poolType, pool);
        }
    }
    
    #region 公共接口
    /// <summary>
    /// 创建UI对象池
    /// </summary>
    /// <param name="poolName">对象池名称</param>
    /// <param name="releaseTime">释放时间</param>
    public void CreateUIPool(float releaseTime) => CreatePool<ObjectPool>(PoolType.UI, releaseTime);

    /// <summary>
    /// 创建资源对象池
    /// </summary>
    /// <param name="releaseTime">释放时间</param>
    public void CreateAssetsPool(float releaseTime) => CreatePool<AssestPool>(PoolType.AssetsBundle,releaseTime);

    /// <summary>
    /// 取出对象
    /// </summary>
    /// <param name="poolName">对象池名称</param>
    /// <param name="assestName">资源名</param>
    /// <returns></returns>
    public Object TakeObject(PoolType type, string assestName)
    {
        if (this.Pools.TryGetValue(type, out ObjectBase pool))
            return pool.Take(assestName);
        else
            return null;
    }

    /// <summary>
    /// 回收对象
    /// </summary>
    /// <param name="poolName">对象池名称</param>
    /// <param name="assestName">资源名</param>
    /// <param name="asset">对象</param>
    public void RecycleObject(PoolType poolType, string assestName,UnityEngine.Object asset)
    {
        if (Pools.TryGetValue(poolType, out ObjectBase pool))
            pool.Recycle(assestName, asset);
        else
            Debug.Log(assestName + "RecycleObject Exception!!!");
    }
    #endregion
}
