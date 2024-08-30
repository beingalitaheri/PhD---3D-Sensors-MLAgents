using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomingMissile : MonoBehaviour
{
    [SerializeField] private float speed;

    [SerializeField] private Transform targetTransform;
    private GameObject ob;
    private void Start()
    {
        if (targetTransform == null) 
        {
            ob = GameObject.FindWithTag("Spaceship");
            targetTransform = ob.transform;
        }
    }
    private void Update()
    {
        var step =  speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, targetTransform.position, step);
    }
}
