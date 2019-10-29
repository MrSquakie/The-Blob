using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class YouWin : MonoBehaviour
{
    private ScoreText scoreText => GetComponent<ScoreText>();
    public GameObject winText;
    public bool win = false; 


    public void Update()
    {
        if (scoreText.score >= 12)
        {
            winText.SetActive(true);
            Timer();
            win = true;
        }

        if (win)
        {
            Timer();
        }
    }

    public float timer = 1f;

    void Timer()
    {
        if (timer <= 0)
        {
            Application.Quit();
            timer = 1f;
        }
        else
        {
            timer -= Time.deltaTime;
        }
        print(timer);
    }
}
