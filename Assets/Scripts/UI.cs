using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI : MonoBehaviour
{
    
    public Slider weightSlider;
    public TextMeshProUGUI coinCount;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        weightSlider.maxValue = GameObject.FindGameObjectsWithTag("Coin").Length;
        coinCount.text = "Coins: " + GlobalVariables.currentCoins;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateCoinCount()
    {
        coinCount.text = "Coins: " + GlobalVariables.currentCoins;
        weightSlider.value = GlobalVariables.currentCoins;
    }

}
