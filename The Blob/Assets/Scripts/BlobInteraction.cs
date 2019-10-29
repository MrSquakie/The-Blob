using System.Collections;
using System.Collections.Generic;
using Obi;
using UnityEngine;

public class BlobInteraction : MonoBehaviour
{

    ObiSolver solver;
    private MeshRenderer meshRender => GetComponent<MeshRenderer>();
    private Material material => meshRender.material;
    private Color matAlpha => material.color;

    Obi.ObiSolver.ObiCollisionEventArgs collisionEvent;

    void Awake()
    {
        solver = GetComponent<Obi.ObiSolver>();
    }

    void OnEnable()
    {
        solver.OnCollision += Solver_OnCollision;
    }

    void OnDisable()
    {
        solver.OnCollision -= Solver_OnCollision;
    }

    void Solver_OnCollision(object sender, Obi.ObiSolver.ObiCollisionEventArgs e)
    {
        foreach (Oni.Contact contact in e.contacts)
        {
            // this one is an actual collision:
            if (contact.distance < 0.01)
            {
                Component collider;
                if (ObiCollider.idToCollider.TryGetValue(contact.other, out collider))
                {
                    if (collider.transform.name == "Player")
                    {
                        Health health = collider.GetComponent<Health>();
                        health.TakeDamge(5f);
                        Destroy(gameObject);
                    }
                }
            }
        }
    }
}
