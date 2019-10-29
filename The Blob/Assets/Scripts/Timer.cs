using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    public float targetTime = 180f;

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

        timerText.text = targetTime.ToString();

        if(targetTime <= 0)
        {
            SceneManager.LoadScene(0);
        }
    }
}
