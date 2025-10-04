using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coins : MonoBehaviour
{
    UI uiScript;
    private CapsuleCollider2D Collider;
    private int coinScript = 0;
    public bool isCharging = false;
    public float startTimer = 5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        uiScript = GameObject.FindGameObjectWithTag("UI").GetComponent<UI>();
        Collider = GetComponent<CapsuleCollider2D>();
    }

    // Update is called once per frame
    void Update()
    {
       
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        
        if (col.gameObject.CompareTag("Player") && (!isCharging))
        {
            col.GetComponent<MovementController>().DecreasePlayerSpeed();
            coinScript++;
            Debug.Log("Trigger " + coinScript);
            GlobalVariables.currentCoins++;
            uiScript.UpdateCoinCount();
            Destroy(gameObject);
            Debug.Log("Coin Count: " + GlobalVariables.currentCoins);
            isCharging = true;
            GlobalVariables.Timer(ref isCharging, ref startTimer);
        }
        
        

    }

}

