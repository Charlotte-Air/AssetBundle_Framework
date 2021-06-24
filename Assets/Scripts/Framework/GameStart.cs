using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStart : MonoBehaviour
{
    public GameMode GameMode;

    void Awake()
    {
        AppConst.gameMode = this.GameMode;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
