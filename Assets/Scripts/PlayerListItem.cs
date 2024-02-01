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
    Player player;

    public void SetUp(Player _player)
    {
        player = _player;
        text = GameObject.Find("Player" + (player.IsMasterClient ? "1" : "2")).GetComponent<TMP_Text>();
        text.text = player.NickName;
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

    public override void OnLeftRoom()
    {
        Destroy(gameObject);
    }
}
