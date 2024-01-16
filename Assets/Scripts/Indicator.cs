using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Indicator: MonoBehaviour
{
    GameObject targetFollow;
    GameObject host;
    PlayerController targetPlayerController;
    PlayerController playerController;
    [SerializeField] GameObject[] directions;
    public void InitTarget(GameObject target, GameObject _host)
    {
        targetFollow = target;
        targetPlayerController = targetFollow.GetComponent<PlayerController>();
        host = _host;
        //playerController = host.GetComponent<PlayerController>();
    }
    private void Update()
    {
        if (targetFollow != null)
        {
            if (targetPlayerController.isDead)
            {
                Destroy(gameObject);
                return;
            }

            transform.position = Camera.main.WorldToScreenPoint(targetFollow.transform.position);
            // check if enemy is facing player
            Vector3 directionToPlayer = targetFollow.transform.position - host.transform.position;
            float dotProduct = Vector3.Dot(targetPlayerController.orientation.forward, directionToPlayer.normalized);

            // if facing player swap left and right to show correctly
            if (targetPlayerController.isAttacking)
            {
                if (targetPlayerController.canParry)
                {
                    ChangeIndicatorColor(new Color(0, 1, 0, 100));
                }
                else
                {
                    ChangeIndicatorColor(new Color(1, 0, 0, 100));
                }
            }
            else if(targetPlayerController.isStaggered)
            {
                ChangeIndicatorColor(new Color(0, 0, 0, 100));
            }
            else
            {
                ChangeIndicatorColor(new Color(1, 1, 1, 100));
            }
            if (!targetPlayerController.isAttacking)
            { 
                if (dotProduct > 0f)
                {
                    if (!directions[0].activeInHierarchy && targetPlayerController.GetDir() == MouseController.DirectionalInput.TOP)
                    {
                        directions[0].SetActive(true);
                        directions[1].SetActive(false);
                        directions[2].SetActive(false);
                    }
                    else if (!directions[1].activeInHierarchy && targetPlayerController.GetDir() == MouseController.DirectionalInput.LEFT)
                    {
                        directions[0].SetActive(false);
                        directions[1].SetActive(true);
                        directions[2].SetActive(false);
                    }
                    else if (!directions[2].activeInHierarchy && targetPlayerController.GetDir() == MouseController.DirectionalInput.RIGHT)
                    {
                        directions[0].SetActive(false);
                        directions[1].SetActive(false);
                        directions[2].SetActive(true);
                    }
                }
                else
                {
                    if (!directions[0].activeInHierarchy && targetPlayerController.GetDir() == MouseController.DirectionalInput.TOP)
                    {
                        directions[0].SetActive(true);
                        directions[1].SetActive(false);
                        directions[2].SetActive(false);
                    }
                    else if (!directions[2].activeInHierarchy && targetPlayerController.GetDir() == MouseController.DirectionalInput.LEFT)
                    {
                        directions[0].SetActive(false);
                        directions[1].SetActive(false);
                        directions[2].SetActive(true);
                    }
                    else if (!directions[1].activeInHierarchy && targetPlayerController.GetDir() == MouseController.DirectionalInput.RIGHT)
                    {
                        directions[0].SetActive(false);
                        directions[1].SetActive(true);
                        directions[2].SetActive(false);
                    }

                }
            }
        }
        else
        {
            Destroy(this);
        }
    }

    private void ChangeIndicatorColor(Color _color)
    {
        for(int i = 0; i < 3; ++i)
        {
            directions[i].GetComponent<Image>().color = _color;
        }
    }
    public GameObject GetTarget()
    {
        return targetFollow;
    }
}
