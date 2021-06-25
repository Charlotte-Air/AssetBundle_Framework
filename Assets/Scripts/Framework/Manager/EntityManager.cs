using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityManager : MonoBehaviour
{
    /// <summary>
    /// 缓存集合
    /// </summary>
    private Dictionary<string, GameObject> Entitys = new Dictionary<string, GameObject>();
    /// <summary>
    /// 分组集合
    /// </summary>
    private Dictionary<string, Transform> Groups = new Dictionary<string, Transform>();
    private Transform EntityParent;

    void Awake()
    {
        EntityParent = this.transform.parent.Find("Entity");
    }

    /// <summary>
    /// 设置实体分组
    /// </summary>
    /// <param name="group"></param>
    public void SetEnitiyGroup(List<string> group)
    {
        for (int i = 0; i < group.Count; i++)
        {
            GameObject go = new GameObject("Group-" + group[i]);
            go.transform.SetParent(EntityParent, false);
            Groups.Add(group[i], go.transform);
        }
    }

    /// <summary>
    /// 获取实体分组
    /// </summary>
    /// <param name="group"></param>
    /// <returns></returns>
    Transform GetEnitiyGroup(string group)
    {
        if (!Groups.ContainsKey(group))
            Debug.LogError("Group is Not Exist");
        return Groups[group];
    }

    /// <summary>
    /// 显示实体
    /// </summary>
    /// <param name="uiName">UI名称</param>
    /// <param name="group">分组类型名称</param>
    /// <param name="luaName">Lua脚本名称</param>
    public void ShowEntity(string Name, string group, string luaName)
    {
        GameObject entity = null;
        if (Entitys.TryGetValue(Name, out entity)) //查找集合是否已经加载过
        {
            Entity logic = entity.GetComponent<Entity>();
            logic.OnShow();
            return;
        }

        Manager.Resourece.LoadPrefab(Name, (UnityEngine.Object obj) =>
        {
            entity = Instantiate(obj) as GameObject;
            Entitys.Add(Name, entity);
            Transform parent = GetEnitiyGroup(group);
            parent.transform.SetParent(parent, false);
            Entity en = entity.AddComponent<Entity>();
            en.Init(luaName);
            en.OnShow();
        });
    }
}
