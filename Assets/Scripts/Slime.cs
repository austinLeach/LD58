using UnityEngine;

public class Slime : MonoBehaviour
{
    private float speed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter2D(Collider2D col)
    {

        if (col.gameObject.CompareTag("Player"))
        {
            GlobalVariables.currentCoins++;
            uiScript.UpdateCoinCount();
            Destroy(gameObject);

        }



    }


}
