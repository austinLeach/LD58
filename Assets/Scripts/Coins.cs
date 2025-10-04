using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coins : MonoBehaviour
{

    private CapsuleCollider2D Collider;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       
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
            PlayerCoins.CollectCoin();
    }

}

