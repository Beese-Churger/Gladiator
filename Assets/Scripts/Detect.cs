using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Detect : MonoBehaviour
{
    [SerializeField] Collider radius;
    public List<GameObject> opponentsInRange = new();

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        opponentsInRange.Add(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        opponentsInRange.Remove(other.gameObject);
    }
}
