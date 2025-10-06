using UnityEngine;

public class Follow_player : MonoBehaviour {

    public Transform player;
    public bool thankYou = false;

    // Update is called once per frame
    void Update () {
        if (thankYou)
        {
            transform.position = player.transform.position + new Vector3(0, 1, -15);
            return;
        }
        else
        {
            transform.position = player.transform.position + new Vector3(0, -1, -8);
        }
    }
}