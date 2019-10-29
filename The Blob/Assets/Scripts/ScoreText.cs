using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreText : MonoBehaviour
{
    public Text scoreText;

    public int score;
    private int goodCount;

    public AudioSource collectSound => GetComponent<AudioSource>();

    private void Start()
    {
        score = 0;
        goodCount = GameObject.FindGameObjectsWithTag("Good").Length;
        scoreText.text = "Score: " + score + "/" + goodCount;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Good"))
        {
            score++;
            scoreText.text = "Score: " + score + "/" + goodCount;
            collectSound.Play();
            Destroy(other.gameObject);
        }
    }
}
