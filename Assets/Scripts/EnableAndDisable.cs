using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class EnableAndDisable : MonoBehaviour
{

    public float FirstorSecond;
    Image img;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        img = gameObject.GetComponent<Image>();
        img.enabled = false;
        Debug.Log("Image is disabled");
    }

    // Update is called once per frame
    public void abilityUpdate()
    {
        if (GlobalVariables.currentCoins / GlobalVariables.levelCoins >= FirstorSecond/3f)
        {
            img.enabled = true;
            Debug.Log("Image is enabled");
        }
    }
}
