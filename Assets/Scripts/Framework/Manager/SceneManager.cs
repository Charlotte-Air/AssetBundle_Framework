using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManager : MonoBehaviour
{
    private string Name = "[Scene]";

    void Awake()
    {
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    /// <summary>
    /// 场景切换回调
    /// </summary>
    /// <param name="s1">场景1</param>
    /// <param name="s2">场景2</param>
    private void OnActiveSceneChanged(UnityEngine.SceneManagement.Scene s1, UnityEngine.SceneManagement.Scene s2)
    {
        if (!s1.isLoaded || !s2.isLoaded)
            return;

        Scene se1 = GetScene(s1);
        Scene se2 = GetScene(s2);
        se1?.OnInActive();
        se2?.OnActive();
    }

    /// <summary>
    /// 激活场景
    /// </summary>
    /// <param name="sceneName">场景名</param>
    public void SetActive(string sceneName)
    {
        UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
    }

    /// <summary>
    /// 获取场景
    /// </summary>
    /// <param name="scene">场景名</param>
    /// <returns></returns>
    private Scene GetScene(UnityEngine.SceneManagement.Scene scene)
    {
        GameObject[] go = scene.GetRootGameObjects();
        foreach (GameObject obj in go)
        {
            if (obj.name.CompareTo(Name) == 0)
            {
                Scene se = obj.GetComponent<Scene>();
                return se;
            }
        }
        return null;
    }

    /// <summary>
    /// 判断场景是否已加载
    /// </summary>
    /// <param name="sceneName">场景名</param>
    /// <returns></returns>
    private bool IsLoadedScene(string sceneName)
    {
        UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
        return scene.isLoaded;
    }

    /// <summary>
    /// 叠加加载场景
    /// </summary>
    /// <param name="sceneName">场景名</param>
    /// <param name="luaName">Lua脚本名</param>
    public void LoadScene(string sceneName, string luaName)
    {
        Manager.Resourece.LoadScone(sceneName,(UnityEngine.Object obj)=>
        {
            StartCoroutine(StartLoadScene(sceneName, luaName, LoadSceneMode.Additive));
        });
    }

    /// <summary>
    /// 切换场景
    /// </summary>
    /// <param name="sceneName">场景名</param>
    /// <param name="luaName">Lua脚本名</param>
    public void ChangeScene(string sceneName, string luaName)
    {
        Manager.Resourece.LoadScone(sceneName, (UnityEngine.Object obj) =>
        {
            StartCoroutine(StartLoadScene(sceneName, luaName, LoadSceneMode.Single));
        });
    }

    /// <summary>
    /// 异步加载场景
    /// </summary>
    /// <param name="sceneName">场景名称</param>
    /// <param name="luaName">Lua脚本名</param>
    /// <param name="mode">加载模式</param>
    /// <returns></returns>
    IEnumerator StartLoadScene(string sceneName, string luaName, LoadSceneMode mode)
    {
        if(IsLoadedScene(sceneName))
            yield break;

        AsyncOperation async = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, mode);
        async.allowSceneActivation = true; //自动跳转场景
        yield return async;

        UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName); //获取场景对象
        GameObject go = new GameObject(Name);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene); //移动场景
        
        Scene se = go.AddComponent<Scene>();
        se.SceneName = sceneName;
        se.Init(luaName);
        se.OnEnter();
    }


    /// <summary>
    /// 卸载场景
    /// </summary>
    /// <param name="sceneName">场景名</param>
    public void UnLoadSceneAsync(string sceneName)
    {
        StartCoroutine(UnLoadScene(sceneName));
    }

    /// <summary>
    /// 异步卸载场景
    /// </summary>
    /// <param name="sceneName">场景名</param>
    /// <returns></returns>
    IEnumerator UnLoadScene(string sceneName)
    {
        UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded)
        {
            Debug.LogError("Scene No isLoaded");
            yield break;
        }
        Scene se = GetScene(scene);
        se?.OnQuit();
        AsyncOperation async = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
        yield return async;
    }

}
