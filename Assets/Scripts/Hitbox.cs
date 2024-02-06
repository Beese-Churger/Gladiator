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
        Vector3 Hitpos = other.ClosestPointOnBounds(transform.position);
        if (other.CompareTag("Player") && other.gameObject != playerController.gameObject)
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
                if(target.cameraController.CombatMode && target.CheckIfBlocked(playerController, playerController.GetDir()))
                {
                    if(!playerController.isHeavy)
                    {
                        playerController.LightStagger();
                    }
                    target.BlockAttack(playerController.isHeavy, Hitpos);
                    
                }
                else
                {
                    Debug.Log(playerController.currentAttack);
                    if(!target.isInvincible || !target.cameraController.CombatMode)
                        target.TakeDamage(playerController.attackDictionary[playerController.currentAttack], Hitpos);
                }
            }
                
            //target.TakeDamage(10);
        }
    }
}
