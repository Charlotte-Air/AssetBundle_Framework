using UnityEngine;

public class Gameobject
{
    public Object Object; //具体对象
    public string Name; //对象名称
    public System.DateTime LastUserTime; //销毁时间
    public Gameobject(string name, Object obj)
    {
        Name = name;
        Object = obj;
        LastUserTime = System.DateTime.Now;
    }
}
