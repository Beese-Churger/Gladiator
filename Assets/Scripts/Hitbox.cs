using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
public class Hitbox : MonoBehaviour
{
    [SerializeField] PlayerController playerController;
    [SerializeField] PhotonView PV;


    private void Awake()
    {
        PV = GetComponent<PhotonView>();
    }

    private void Update()
    {

    }
    private void OnTriggerEnter(Collider other)
    {
        if (!PV.IsMine)
            return;
        if(other.CompareTag("Player") && other.gameObject != playerController.gameObject)
        {
            //Debug.Log($"Player {photonView.Owner.NickName} took {input}");
            //Debug.Log("hit");

            PlayerController target = other.GetComponent<PlayerController>();

            if(!target.isParrying || target.isAttacking)
            {
                if (target.CheckIfBlocked(playerController, playerController.GetDir(), 10, false))
                {
                    target.BlockAttack(10, false);
                }
                else
                    target.TakeDamage(10);
            }

            //target.TakeDamage(10);
        }
    }
}
