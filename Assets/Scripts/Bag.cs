using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Bag : MonoBehaviour
{
    [SerializeField] SpriteRenderer smallSprite;
    [SerializeField] SpriteRenderer mediumSprite;
    [SerializeField] SpriteRenderer largeSprite;
    private bool medium = false;
    private bool large = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        smallSprite.enabled = true;
        mediumSprite.enabled = false;
        largeSprite.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        float maxVal = GlobalVariables.levelCoins;
        float curVal = GlobalVariables.currentCoins;
        float currentCoinsPercent = curVal / maxVal;

        if (!medium && currentCoinsPercent >= 1f / 3f)
        {
            smallSprite.enabled = false;
            mediumSprite.enabled = true;
            largeSprite.enabled = false;
            medium = true;
        }

        if (!large && currentCoinsPercent >= 2f / 3f)
        {
            smallSprite.enabled = false;
            mediumSprite.enabled = false;
            largeSprite.enabled = true;
            large = true;
        }

    }
}
