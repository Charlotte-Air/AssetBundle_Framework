using UnityEngine;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    Transform ui_Transform;
    Dictionary<string, Transform> UIGroups;

    void Awake()
    {
        ui_Transform = this.transform.parent.Find("UI");
        Init();
    }

    void Init()
    {
        UIGroups = new Dictionary<string, Transform>();
    }

    /// <summary>
    /// 设置UI层级
    /// </summary>
    /// <param name="group"></param>
    public void SetGroup(List<string> group)
    {
        for (int i = 0; i < group.Count; i++)
        {
            GameObject go = new GameObject("Group-" + group[i]);
            go.transform.SetParent(ui_Transform, false);
            UIGroups.Add(group[i], go.transform);
        }
    }

    /// <summary>
    /// 获取UI分组
    /// </summary>
    /// <param name="group"></param>
    /// <returns></returns>
    Transform GetGroup(string group)
    {
        if (!UIGroups.ContainsKey(group))
            Debug.LogError(group + "GetGroup Exception!!!");
        return UIGroups[group];
    }

    /// <summary>
    /// 显示UI
    /// </summary>
    /// <param name="uiName">UI名称</param>
    /// <param name="group">分组类型名称</param>
    /// <param name="luaName">Lua脚本名称</param>
    public void ShowUI(string uiName,string group,string luaName)
    {
        GameObject ui = null;
        Transform parent = GetGroup(group);
        string uiPath= PathUtil.GetUIPath(uiName);
        Object uiObj = GameManager.Instance.GetManager<ObjectPoolManager>(GameManager.ManagerName.Pool).TakeObject(PoolType.UI,uiPath);

        if (uiObj != null)
        {
            ui = uiObj as GameObject;
            ui.transform.SetParent(parent, false);
            UI openui = ui.GetComponent<UI>();
            openui.OnOpen();
            openui.CharacterInit();
            return;
        }

        GameManager.Instance.GetManager<ResoureceManager>(GameManager.ManagerName.Resourece).LoadUI(uiName, (Object obj) =>
        {
            ui =Instantiate(obj) as GameObject;
            ui.transform.SetParent(parent, false);
            UI baseUI  = ui.AddComponent<UI>();
            baseUI.AssestName = uiPath;
            baseUI.Init(luaName);
            baseUI.OnOpen();
            baseUI.CharacterInit();
        });
    }
}
