using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.IO;
using System.Linq;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerManager : MonoBehaviour
{
    PhotonView PV;

    GameObject controller;

    int kills;
    int deaths;

    List<Transform> spawnPoints = new();

    private void Awake()
    {
        PV = GetComponent<PhotonView>();
        Transform arenaSpawns = GameObject.Find("Spawns").transform;
        foreach(Transform spawnPoint in arenaSpawns)
        {
            spawnPoints.Add(spawnPoint);
        }
    }

    private void Start()
    {
        if(!PV.IsMine)
            return;

        CreateController();
        
    }

    void CreateController()
    {
        PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Player"), spawnPoints[PV.Owner.ActorNumber].position, Quaternion.identity);
    }


    public void Die()
    {
        PhotonNetwork.Destroy(controller);
        //CreateController();

        deaths++;

        Hashtable hash = new Hashtable();
        hash.Add("deaths", deaths);
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
    }

    public void GetKill()
    {
        PV.RPC(nameof(RPC_GetKill), PV.Owner);
    }

    [PunRPC]
    void RPC_GetKill()
    {
        kills++;

        Hashtable hash = new Hashtable();
        hash.Add("kills", kills);
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
    }

    public static PlayerManager Find(Player player)
    {
        return FindObjectsOfType<PlayerManager>().SingleOrDefault(x => x.PV.Owner == player);
    }
}
