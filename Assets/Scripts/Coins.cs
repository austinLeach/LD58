using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coins : MonoBehaviour
{
    UI uiScript;
    private CapsuleCollider2D Collider;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        uiScript = GameObject.FindGameObjectWithTag("UI").GetComponent<UI>();
        Collider = GetComponent<CapsuleCollider2D>();
    }

    // Update is called once per frame
    void Update()
    {
       //Check for if player is colliding with coin 
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        Destroy(gameObject);
        GlobalVariables.currentCoins += 1;
        uiScript.UpdateCoinCount();
        Debug.Log("Coin Count: " + GlobalVariables.currentCoins);

    }

}

