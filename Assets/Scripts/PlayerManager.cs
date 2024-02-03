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


    private void Awake()
    {
        PV = GetComponent<PhotonView>();
    }

    private void Start()
    {
        if(!PV.IsMine)
            return;

        CreateController();
        
    }

    void CreateController()
    {
        int team = PhotonNetwork.LocalPlayer.IsMasterClient ? 1 : 2;
        Hashtable hash = new Hashtable();
        hash.Add("team", team);
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);

        Transform spawnPoint = SpawnManager.Instance.GetSpawnPoint(team);

        controller = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Player"), spawnPoint.position, spawnPoint.rotation);
       
    }

    public Transform RespawnPoint()
    {
        return SpawnManager.Instance.GetSpawnPoint(PV.Owner.ActorNumber);
    }

    public void Die()
    {
        PhotonNetwork.Destroy(controller);
        //CreateController();
    }

    public void DeathCount()
    {
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
