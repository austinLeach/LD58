using UnityEngine;
using TMPro;

public class totalCoinsAtEnd : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI totalCollectedText; // Assign your TMP text component in inspector
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Update the text with the global total collected value when scene loads
        UpdateTotalCollectedDisplay();
    }
    
    private void UpdateTotalCollectedDisplay()
    {
        if (totalCollectedText != null)
        {
            totalCollectedText.text = totalCollectedText.text +GlobalVariables.totalCollected.ToString();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
