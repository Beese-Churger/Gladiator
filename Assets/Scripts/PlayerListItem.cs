using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
public class PlayerListItem : MonoBehaviourPunCallbacks
{
    [SerializeField] TMP_Text text;
    [SerializeField] GameObject model;
    [SerializeField] ControllerHolder controller;
    [SerializeField] Animator animator;
    Player player;

    public void SetUp(Player _player)
    {
        player = _player;
        text = GameObject.Find("Player" + (player.IsMasterClient ? "1" : "2")).GetComponent<TMP_Text>();
        text.text = player.NickName;

        foreach (GameObject weapons in controller.weapons)
        {
            weapons.SetActive(false);
        }
        foreach (GameObject helms in controller.helms)
        {
            helms.SetActive(false);
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if(player == otherPlayer)
        {
            text.text = "Waiting...";
            Destroy(gameObject);
            return;
        }
        else
        {
            if(player.IsMasterClient)
            {
                text.text = "Waiting...";
                model.transform.position = GameObject.Find("player1pos").transform.position;
                text = GameObject.Find("Player1").GetComponent<TMP_Text>();
                text.text = player.NickName;
            }
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        // Check if the player's custom properties contain the key we are interested in
        if (changedProps.ContainsKey(GladiatorInfo.PLAYER_WEAPON))
        {
            if (targetPlayer == player)
            {
                foreach (GameObject weapons in controller.weapons)
                {
                    weapons.SetActive(false);
                }
                foreach (GameObject helms in controller.helms)
                {
                    helms.SetActive(false);
                }
                int weaponid = int.Parse(changedProps[GladiatorInfo.PLAYER_WEAPON].ToString());
                controller.helms[weaponid].SetActive(true);
                controller.weapons[weaponid].SetActive(true);
                animator.runtimeAnimatorController = controller.animators[weaponid];
            }
        }
    }

    public override void OnLeftRoom()
    {
        Destroy(gameObject);
    }
}
