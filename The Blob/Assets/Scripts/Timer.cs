using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    public float targetTime = 180f;

    public Text timerText;

    // Start is called before the first frame update
    void Start()
    {
        timerText.text = "";
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
