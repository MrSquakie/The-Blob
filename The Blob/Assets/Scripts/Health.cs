using System.Collections;
using System.Collections.Generic;
using Obi;
using UnityEngine;

public class Health : MonoBehaviour
{

    public float health;
    public ObiSolver solver;
    public ObiCollider collider => GetComponent<ObiCollider>();

    public void TakeDamge(float damageAmount)
    {
        health -= damageAmount;
        print(health);
    }


    public void OnTriggerEnter(Collider other)
    {
    

         print("hit");

        
    }



}
