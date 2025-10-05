using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalVariables : MonoBehaviour
{
    public static int totalCollected = 0;
    public static int currentCoins = 0;
    public static int levelCoins = 0;


    private void Start()
    {
        levelCoins = GameObject.FindGameObjectsWithTag("Coin").Length;
    }

    public static bool Timer(ref bool isChanging, ref float timer)
    {
        if (isChanging)
        {
            timer -= Time.deltaTime;
            if (timer < 0)
            {
                isChanging = false;
            }
        }
        return isChanging;
    }

}
