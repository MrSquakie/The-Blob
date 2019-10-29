using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreText : MonoBehaviour
{
    public Rigidbody player;

    // Update is called once per frame
    void Update()
    {
        GetComponent<Text>().text = "Score: " + player.mass;
    }
}
