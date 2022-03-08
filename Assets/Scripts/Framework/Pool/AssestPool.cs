using UnityEngine;

public class AssestPool : ObjectBase
{
    public override Object Take(string name) => base.Take(name);
    public override void Recycle(string name, Object obj) => base.Recycle(name, obj);
    public override void Release()
    {
        base.Release();
        var pool = base.objectPools.GetEnumerator();
        while (pool.MoveNext())
        {
            if (System.DateTime.Now.Ticks - pool.Current.LastUserTime.Ticks >= ReleaseTime * 10000000)
            {
                Debug.LogFormat("AssestPool Name:{0} Time:{1}", pool.Current.Name, System.DateTime.Now);
                GameManager.Instance.GetManager<ResoureceManager>(GameManager.ManagerName.Resourece).ReleaseBundle(pool.Current.Object); //释放Bundle
                objectPools.Remove(pool.Current);//移除对象池
                Release(); //递归自己跳出循环
                return;
            }
        }
    }
}
