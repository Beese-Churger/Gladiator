using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Indicator: MonoBehaviour
{
    GameObject targetFollow;

    public void InitTarget(GameObject target)
    {
        targetFollow = target;
    }
    private void Update()
    {
        if (targetFollow != null)
        {
            transform.position = Camera.main.WorldToScreenPoint(targetFollow.transform.position);
        }
        else
        {
            Destroy(this);
        }
    }

    public GameObject GetTarget()
    {
        return targetFollow;
    }
}
