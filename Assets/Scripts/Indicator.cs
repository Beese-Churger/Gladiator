using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Indicator: MonoBehaviour
{
    GameObject targetFollow;
    PlayerController playerController;
    [SerializeField] GameObject[] directions;
    public void InitTarget(GameObject target)
    {
        targetFollow = target;
        playerController = targetFollow.GetComponent<PlayerController>();
    }
    private void Update()
    {
        if (targetFollow != null)
        {
            transform.position = Camera.main.WorldToScreenPoint(targetFollow.transform.position);
            if (!directions[0].activeInHierarchy && playerController.GetDir() == MouseController.DirectionalInput.TOP)
            {
                directions[0].SetActive(true);
                directions[1].SetActive(false);
                directions[2].SetActive(false);
            }
            else if (!directions[1].activeInHierarchy && playerController.GetDir() == MouseController.DirectionalInput.LEFT)
            {
                directions[0].SetActive(false);
                directions[1].SetActive(true);
                directions[2].SetActive(false);
            }
            else if (!directions[2].activeInHierarchy && playerController.GetDir() == MouseController.DirectionalInput.RIGHT)
            {
                directions[0].SetActive(false);
                directions[1].SetActive(false);
                directions[2].SetActive(true);
            }
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
