using UnityEngine;
using System.Collections.Generic;

public class EntityManager : MonoBehaviour
{
    Transform EntityParent;
    Dictionary<string, Transform> groups;   //分组集合
    Dictionary<string, GameObject> entitys; //缓存集合

    void Awake()
    {
        EntityParent = this.transform.parent.Find("Entity");
        Init();
    }
    
    void Init()
    {
        entitys = new Dictionary<string, GameObject>(); //缓存集合
        groups = new Dictionary<string, Transform>();
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
            groups.Add(group[i], go.transform);
        }
    }

    /// <summary>
    /// 获取实体分组
    /// </summary>
    /// <param name="group"></param>
    /// <returns></returns>
    Transform GetEnitiyGroup(string group)
    {
        if (!groups.ContainsKey(group))
            Debug.LogError(group + "GetGroup Exception!!!");
        return groups[group];
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
        if (entitys.TryGetValue(Name, out entity)) //查找集合是否已经加载过
        {
            Entity logic = entity.GetComponent<Entity>();
            logic.OnShow();
            return;
        }
        GameManager.Instance.GetManager<ResoureceManager>(GameManager.ManagerName.Resourece).LoadPrefab(Name, (UnityEngine.Object obj) =>
        {
            entity = Instantiate(obj) as GameObject;
            entitys.Add(Name, entity);
            Transform parent = GetEnitiyGroup(group);
            parent.transform.SetParent(parent, false);
            Entity en = entity.AddComponent<Entity>();
            en.Init(luaName);
            en.OnShow();
        });
    }
}
