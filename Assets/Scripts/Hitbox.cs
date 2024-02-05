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

            if(playerController.isBash)
            {
                if (!target.isInvincible)
                    target.GiveBash();
            }
            else if(!target.isParrying)
            {
                if(target.CheckIfBlocked(playerController, playerController.GetDir()))
                {
                    if(!playerController.isHeavy)
                    {
                        playerController.LightStagger();
                    }
                    target.BlockAttack(playerController.isHeavy);
                    
                }
                else
                {
                    Debug.Log(playerController.currentAttack);
                    if(!target.isInvincible)
                        target.TakeDamage(playerController.attackDictionary[playerController.currentAttack]);
                }
            }
                
            //target.TakeDamage(10);
        }
    }
}
