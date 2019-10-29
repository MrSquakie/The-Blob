using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlobSpawner : MonoBehaviour
{

    public Vector3 center;
    public Vector3 size;
    public float timer = 1f; 

    public GameObject BlobPrefab;

    void Start()
    {

    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.M))
        {
            SpawnItem();
        }


        Timer();
    }

    public void SpawnItem()
    {
        Vector3 pos = center + new Vector3(Random.Range(-size.x / 2, size.x / 2), Random.Range(-size.y / 2, size.y / 2),
                          Random.Range(-size.z / 2, size.z / 2));



        Instantiate(BlobPrefab, pos, Quaternion.identity);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawCube(center, size);
    }



    void Timer()
    {
        if (timer <= 0 )
        {
            SpawnItem();
            timer = 1f;
        }
        else
        {
            timer -= Time.deltaTime;
        }
    }
}
