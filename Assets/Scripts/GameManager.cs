using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;
    PhotonView PV;
    public GameStates gameState;

    public List<PlayerController> team1Players = new();
    public List<PlayerController> team2Players = new();

    public PlayerController masterClient;

    [SerializeField] Score scoreboard;

    CountdownTimer countdownTimer;
    [SerializeField] RoundTimer roundTimer;

    int round = 1;
    int MAXROUNDS = 5;
    int team1Points = 0;
    int team2Points = 0;
    int MAXPOINTS = 3;
    int winningTeam = 0;

    int crowdFavour = 50;
    int MAXFAVOUR = 100;
    int MINFAVOUR = 0;
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

        ExitGames.Client.Photon.Hashtable props = new()
        {
            {GladiatorInfo.PLAYER_LOADED_LEVEL, true}
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        //Debug.Log("set");
        //if (!PhotonNetwork.IsMasterClient)
        //    return;

        CountdownTimer.OnCountdownTimerHasExpired += OnCountdownTimerIsExpired;
        RoundTimer.OnRoundTimerHasExpired += OnRoundTimerIsExpired;

        StartCoroutine(WaitToGetPlayers());
    }
    
    private void Awake()
    {

        PV = GetComponent<PhotonView>();
        countdownTimer = GetComponent<CountdownTimer>();
        //roundTimer = GetComponent<RoundTimer>();

        gameState = GameStates.COUNTDOWN;
    }
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {

        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }
        //Debug.Log(targetPlayer);

        // if there was no countdown yet, the master client (this one) waits until everyone loaded the level and sets a timer start
        int startTimestamp;
        bool startTimeIsSet = CountdownTimer.TryGetStartTime(out startTimestamp);

        if (changedProps.ContainsKey(GladiatorInfo.PLAYER_LOADED_LEVEL))
        {
            if (CheckAllPlayerLoadedLevel())
            {
                if (!startTimeIsSet)
                {
                    Debug.Log("loaded");
                    CountdownTimer.SetStartTime();
                    //RoundTimer.SetStartTime();
                    PV.RPC(nameof(RPC_StartCountdown), RpcTarget.All);
                }
            }
            else
            {
                // not all players loaded yet. wait:
                Debug.Log("setting text waiting for players! ");
            }
        }

    }
    private bool CheckAllPlayerLoadedLevel()
    {
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            object playerLoadedLevel;

            if (p.CustomProperties.TryGetValue(GladiatorInfo.PLAYER_LOADED_LEVEL, out playerLoadedLevel))
            {
                if ((bool)playerLoadedLevel)
                {
                    continue;
                }
            }
            //Debug.Log(p.NickName + p.CustomProperties.TryGetValue(GladiatorInfo.PLAYER_LOADED_LEVEL, out playerLoadedLevel));
            return false;
        }

        return true;
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

    private void StartGame()
    {
        gameState = GameStates.ROUNDONGOING;
    }
    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        switch (gameState)
        {
            case GameStates.COUNTDOWN:
                if(!countdownTimer.enabled)
                {
                    if(PhotonNetwork.IsMasterClient)
                    {

                    }
                    //countdownTimer.Initialize();
                }
                break;
            case GameStates.ROUNDONGOING:
                if(!roundTimer.enabled)
                {

                }
                //if(timer)
                //{
                //    if(time <= 0)
                //    {
                //        round++;

                //        scoreboard.UpdateScores(team1Points, team2Points, round);
                //        gameState = GameStates.ROUNDOVER;
                //        StartCoroutine(StartNextRound());
                //    }
                //}
                break;
            case GameStates.ROUNDOVER:
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
        PV.RPC(nameof(RPC_RoundOverCall), RpcTarget.MasterClient, team);
    }
    [PunRPC]
    public void RPC_RoundOverCall(int team)
    {
        PV.RPC(nameof(RPC_RoundOver), RpcTarget.All, team);
    }

    [PunRPC]
    public void RPC_RoundOver(int team)
    {
        roundTimer.OnTimerEnds();

        //countdownTimer.OnTimerEnds();
        if (team == 1)
            team1Points++;
        else
            team2Points++;

        //round++;

        scoreboard.UpdateScores(team1Points, team2Points, round);
        gameState = GameStates.ROUNDOVER;

        StartCoroutine(StartNextRound());
    }

    IEnumerator StartNextRound()
    {
        yield return new WaitForSeconds(5f);
        masterClient.Respawn();
        gameState = GameStates.COUNTDOWN;
        round++;
        if(round > MAXROUNDS || team1Points >= 3 || team2Points >= 3)
        {
            GameOver();
            yield break;
        }
        scoreboard.UpdateScores(team1Points, team2Points, round);
        if (PhotonNetwork.IsMasterClient)
        {
            CountdownTimer.SetStartTime();
        }

        yield return new WaitForSeconds(0.1f);
        countdownTimer.enabled = true;
    }
   

    private void OnCountdownTimerIsExpired()
    {
        gameState = GameStates.ROUNDONGOING;
        if (PhotonNetwork.IsMasterClient)
            RoundTimer.SetStartTime();

        //if (!roundTimer)
        //    roundTimer = GetComponent<RoundTimer>();

        //if (Application.isEditor)
            roundTimer.enabled = true;
        //else
        //    StartCoroutine(StartTimer());
    }

    IEnumerator StartTimer()
    {
        yield return new WaitForSeconds(0.1f);
        roundTimer.enabled = true;
    }
    private void OnRoundTimerIsExpired()
    {
        scoreboard.UpdateScores(team1Points, team2Points, round);
        gameState = GameStates.ROUNDOVER;

        //if (team1Points >= 3)
        //    winningTeam = 1;
        //else if (team2Points >= 3)
        //    winningTeam = 2;

        StartCoroutine(StartNextRound());
    }

    [PunRPC]
    public void RPC_StartCountdown()
    {
        countdownTimer.enabled = true;
    }

    public void GameOver()
    {
        gameState = GameStates.POSTGAME;

        if (team1Points > team2Points)
            winningTeam = 1;
        else if (team2Points > team1Points)
            winningTeam = 2;
        else
            winningTeam = 3;

        Debug.Log("end");
        if (PhotonNetwork.IsMasterClient)
        {
            masterClient.ShowWinner(winningTeam);
        }
    }
}
