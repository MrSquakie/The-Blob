using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMass : MonoBehaviour
{
    public float growAmount = 0.30f;
    public float shrinkAmount = 0.30f;
    public float timerShrinkAmount = 0.005f;

    public float shrinkRate = 0.5f;
    
    private float _shrinkCounter;
    private Rigidbody _myRB;

    [SerializeField]
    private string _tagToGrow = "Grow";
    [SerializeField]
    private string _tagToShrink = "Shrink";

    // Start is called before the first frame update
    void Start()
    {
        _myRB = GetComponent<Rigidbody>();

        _shrinkCounter = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        if(Time.time >= _shrinkCounter)
        {
            _myRB.mass -= timerShrinkAmount;
            transform.localScale -= new Vector3(timerShrinkAmount, timerShrinkAmount, timerShrinkAmount);

            _shrinkCounter = Time.time + 2f / shrinkRate;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag(_tagToGrow))
        {
            _myRB.mass += growAmount;
            transform.localScale += new Vector3(growAmount, growAmount, growAmount);

            Destroy(collision.gameObject);
        }

        if (collision.gameObject.CompareTag(_tagToShrink))
        {
            _myRB.mass -= shrinkAmount;
            transform.localScale -= new Vector3(shrinkAmount, shrinkAmount, shrinkAmount);

            Destroy(collision.gameObject);
        }
    }
}
