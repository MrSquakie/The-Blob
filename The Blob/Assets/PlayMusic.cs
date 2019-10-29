using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayMusic : MonoBehaviour
{
    private AudioSource audio => GetComponent<AudioSource>();
    // Start is called before the first frame update
    void Start()
    {
        audio.Play();
    }

}
