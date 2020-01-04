using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    private float targetTime = 120f;
    private int time;

    public Text timerText;

    void Start()
    {
        timerText.text = "";

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        targetTime -= Time.deltaTime;
        time = (int)targetTime;

        timerText.text = time.ToString();

        if(targetTime <= 0)
        {
            SceneManager.LoadScene(0);
        }
    }
}
