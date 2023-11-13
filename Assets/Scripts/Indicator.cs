using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Indicator: MonoBehaviour
{
    GameObject targetFollow;
    GameObject host;
    PlayerController opponentPlayerController;
    PlayerController playerController;
    [SerializeField] GameObject[] directions;
    public void InitTarget(GameObject target, GameObject _host)
    {
        targetFollow = target;
        opponentPlayerController = targetFollow.GetComponent<PlayerController>();
        host = _host;
        //playerController = host.GetComponent<PlayerController>();
    }
    private void Update()
    {
        if (targetFollow != null)
        {
            transform.position = Camera.main.WorldToScreenPoint(targetFollow.transform.position);

            Vector3 directionToPlayer = targetFollow.transform.position - host.transform.position;
            float dotProduct = Vector3.Dot(opponentPlayerController.orientation.forward, directionToPlayer.normalized);

            // if facing player swap left and right to show correctly
            if (dotProduct > 0f) 
            {
                if (!directions[0].activeInHierarchy && opponentPlayerController.GetDir() == MouseController.DirectionalInput.TOP)
                {
                    directions[0].SetActive(true);
                    directions[1].SetActive(false);
                    directions[2].SetActive(false);
                }
                else if (!directions[1].activeInHierarchy && opponentPlayerController.GetDir() == MouseController.DirectionalInput.LEFT)
                {
                    directions[0].SetActive(false);
                    directions[1].SetActive(true);
                    directions[2].SetActive(false);
                }
                else if (!directions[2].activeInHierarchy && opponentPlayerController.GetDir() == MouseController.DirectionalInput.RIGHT)
                {
                    directions[0].SetActive(false);
                    directions[1].SetActive(false);
                    directions[2].SetActive(true);
                }
            }
            else
            {
                if (!directions[0].activeInHierarchy && opponentPlayerController.GetDir() == MouseController.DirectionalInput.TOP)
                {
                    directions[0].SetActive(true);
                    directions[1].SetActive(false);
                    directions[2].SetActive(false);
                }
                else if (!directions[2].activeInHierarchy && opponentPlayerController.GetDir() == MouseController.DirectionalInput.LEFT)
                {
                    directions[0].SetActive(false);
                    directions[1].SetActive(false);
                    directions[2].SetActive(true);
                }
                else if (!directions[1].activeInHierarchy && opponentPlayerController.GetDir() == MouseController.DirectionalInput.RIGHT)
                {
                    directions[0].SetActive(false);
                    directions[1].SetActive(true);
                    directions[2].SetActive(false);
                }
                
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
