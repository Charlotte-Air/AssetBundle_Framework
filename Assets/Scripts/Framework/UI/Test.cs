using System.Collections;
using UnityEngine;

public class Test : MonoBehaviour
{
    IEnumerator Start()
    {
        AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(Application.streamingAssetsPath + "/ui/resources/bg_1.jpg.ab"); //加载资源AB包
        yield return request;

        AssetBundleCreateRequest request1 = AssetBundle.LoadFromFileAsync(Application.streamingAssetsPath + "/ui/resources/character_1.png.ab"); //加载资源AB包
        yield return request1;

        AssetBundleCreateRequest request2 = AssetBundle.LoadFromFileAsync(Application.streamingAssetsPath + "/ui/prefabs/character.prefab.ab"); //加载预设体AB包
        yield return request2;

        AssetBundleRequest bundleRequest = request2.assetBundle.LoadAssetAsync("Assets/BuilResources/UI/prefabs/Character.prefab"); //加载预设资源
        yield return bundleRequest;
        //实例化
        GameObject go =Instantiate(bundleRequest.asset) as GameObject;
        go.transform.SetParent(this.transform);
        go.SetActive(true);
        go.transform.localPosition = Vector3.zero;
    }
}
