using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;
    PhotonView PV;
    public GameStates gameState;

    public List<PlayerController> team1Players = new();
    public List<PlayerController> team2Players = new();

    public PlayerController masterClient;

    int round = 1;
    int MAXROUNDS = 5;
    int team1Points = 0;
    int team2Points = 0;
    int MAXPOINTS = 3;

    public enum GameStates
    {
        PREGAME,
        COUNTDOWN,
        ROUNDOVER,
        ROUNDONGOING,
        POSTGAME,
    }

    private void Start()
    {
        if (!Instance)
            Instance = this;
        else
            Destroy(Instance);

        //if (!PhotonNetwork.IsMasterClient)
        //    return;

        StartCoroutine(WaitToGetPlayers());
    }
    
    private void Awake()
    {
        PV = GetComponent<PhotonView>();

        gameState = GameStates.ROUNDONGOING;
    }

    IEnumerator WaitToGetPlayers()
    {
        yield return new WaitForSeconds(1f);

        PlayerController[] playerControllers = FindObjectsOfType<PlayerController>();
        foreach (PlayerController playerController in playerControllers)
        {
            if (playerController.photonView.IsMine)
                masterClient = playerController;

            if (playerController.team == 1)
            {
                team1Players.Add(playerController);
            }
            else
            {
                team2Players.Add(playerController);
            }
        }
    }
    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        switch(gameState)
        {
            case GameStates.ROUNDONGOING:
                break;
            default:
                break;
        }
    }
    public void GameStart()
    {
        PV.RPC(nameof(RPC_GameStart), RpcTarget.All);
    }

    [PunRPC]
    public void RPC_GameStart()
    {
        gameState = GameStates.COUNTDOWN;
    }

    public void CheckIfRoundEnd()
    {

        bool team1alive = false;
        bool team2alive = false;

        for(int i = team1Players.Count - 1; i >= 0; --i)
        {
            if (!team1Players[i])
            {
                team1Players.RemoveAt(i);
                continue;
            }

            if (!team1Players[i].isDead)
            {
                team1alive = true;
                break;
            }
        }

        for (int i = team2Players.Count - 1; i >= 0; --i)
        {
            if (!team2Players[i])
            {
                team2Players.RemoveAt(i);
                continue;
            }

            if (!team2Players[i].isDead)
            {
                team2alive = true;
                break;
            }
        }
        if (team1alive != team2alive)
        {
            if (team1alive)
            {
                RoundOver(1);
            }
            else
                RoundOver(2);
        }
    }

    public void RoundOver(int team)
    {
        Debug.Log("check");
        PV.RPC(nameof(RPC_RoundOver), RpcTarget.All, team);
    }

    [PunRPC]
    public void RPC_RoundOver(int team)
    {
        if (team == 1)
            team1Points++;
        else
            team2Points++;

        round++;

        masterClient.UpdateScoreboard(team1Points, team2Points, round);
    }
}
