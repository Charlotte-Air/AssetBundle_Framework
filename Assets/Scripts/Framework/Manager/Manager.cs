using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    private static ResoureceManager _resource;
    public static ResoureceManager Resourece { get { return _resource; } }

    private void Awake()
    {
        _resource = this.gameObject.AddComponent<ResoureceManager>();
    }

}
