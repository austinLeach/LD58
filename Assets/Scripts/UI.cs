using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI : MonoBehaviour
{
    
    
    public TextMeshProUGUI coinCount;
    public Slider weightSlider;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        weightSlider.value = 0;
        weightSlider.maxValue = GameObject.FindGameObjectsWithTag("Coin").Length;
        coinCount.text = ""+GlobalVariables.currentCoins + "/"+weightSlider.maxValue;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateCoinCount()
    {
        coinCount.text = ""+GlobalVariables.currentCoins+ "/"+weightSlider.maxValue;
        weightSlider.value = GlobalVariables.currentCoins;
        
        
    }

}
