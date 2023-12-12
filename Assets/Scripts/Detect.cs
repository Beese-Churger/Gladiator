using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Detect : MonoBehaviour
{
    [SerializeField] Collider radius;
    [SerializeField] GameObject indicatorPrefab;
    [SerializeField] Transform canvas;
    [SerializeField] CameraController cameraController;

    public List<GameObject> opponentsInRange = new();

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (opponentsInRange.Contains(other.gameObject))
            return;

        opponentsInRange.Add(other.gameObject);
        GameObject go = Instantiate(indicatorPrefab, Camera.main.WorldToScreenPoint(other.transform.position), Quaternion.identity, canvas);
        go.GetComponent<Indicator>().InitTarget(other.gameObject, gameObject);
        //go.SetActive(false);
        cameraController.indicators.Add(go);
    }

    private void OnTriggerExit(Collider other)
    {
        for(int i = cameraController.indicators.Count - 1; i >= 0; --i)
        {
            if (!opponentsInRange.Contains(cameraController.indicators[i].GetComponent<Indicator>().GetTarget()))
            {
                cameraController.indicators.Remove(cameraController.indicators[i]);
                Destroy(cameraController.indicators[i]);
            }
        }
        cameraController.currentLock = null;
        opponentsInRange.Remove(other.gameObject);
        
    }
}
