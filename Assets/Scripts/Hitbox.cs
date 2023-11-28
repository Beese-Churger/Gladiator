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
            Debug.Log("hit");

            PlayerController target = other.GetComponent<PlayerController>();

            if(!target.isparrying)
                target.CheckIfBlocked(playerController, playerController.GetDir(), 10, false);
            //target.TakeDamage(10);
        }
    }
}
