using UnityEngine;

public class ObjectPool : ObjectBase
{
    public override Object Take(string name)
    {
        Object obj = base.Take(name);
        if (obj == null) { return null; }
        GameObject go = obj as GameObject;
        go.SetActive(true);
        return obj;
    }

    public override void Recycle(string name, Object obj)
    {
        GameObject go = obj as GameObject;
        go.SetActive(false);
        go.transform.SetParent(this.transform,false);
        base.Recycle(name,obj);
    }

    public override void Release()
    {
        base.Release();
        var pool = base.objectPools.GetEnumerator();
        while (pool.MoveNext())
        {
            if (System.DateTime.Now.Ticks - pool.Current.LastUserTime.Ticks >= ReleaseTime * 10000000)
            {
                Debug.LogFormat("ObjectPool Time:{0}", System.DateTime.Now);
                Destroy(pool.Current.Object);
                GameManager.Instance.GetManager<ResoureceManager>(GameManager.ManagerName.Resourece).MinusBundleCount(pool.Current.Name); //对象引用去除
                objectPools.Remove(pool.Current); //移除对象池
                Release(); //递归自己跳出循环
                return;
            }
        }
    }

}
