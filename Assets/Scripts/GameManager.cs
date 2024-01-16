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

    [SerializeField] GameObject countdownTimer;
    [SerializeField] GameObject roundTimer;

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
            { GladiatorInfo.PLAYER_LOADED_LEVEL, true }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        CountdownTimer.OnCountdownTimerHasExpired += OnCountdownTimerIsExpired;
        RoundTimer.OnRoundTimerHasExpired += OnRoundTimerIsExpired;

        StartCoroutine(WaitToGetPlayers());
    }

    private void Awake()
    {

        PV = GetComponent<PhotonView>();
        //countdownTimer = GetComponent<CountdownTimer>();
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
                break;
            case GameStates.ROUNDONGOING:
                break;
            case GameStates.ROUNDOVER:
                break;
            case GameStates.POSTGAME:
                roundTimer.SetActive(false);
                countdownTimer.SetActive(false);
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

        for (int i = team1Players.Count - 1; i >= 0; --i)
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
        roundTimer.GetComponent<RoundTimer>().OnTimerEnds();

        if (team == 1)
            team1Points++;
        else
            team2Points++;

        if (round + 1 > MAXROUNDS || team1Points >= 3 || team2Points >= 3)
        {
            GameOver();
            return;
        }
        
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

        scoreboard.UpdateScores(team1Points, team2Points, round);
        if (PhotonNetwork.IsMasterClient)
        {
            CountdownTimer.SetStartTime();
        }

        countdownTimer.SetActive(true);
        countdownTimer.GetComponent<CountdownTimer>().Initialize();
    }


    private void OnCountdownTimerIsExpired()
    {
        gameState = GameStates.ROUNDONGOING;
        if (PhotonNetwork.IsMasterClient)
            RoundTimer.SetStartTime();

        roundTimer.SetActive(true);
        roundTimer.GetComponent<RoundTimer>().Initialize();
    }

    private void OnRoundTimerIsExpired()
    {
        scoreboard.UpdateScores(team1Points, team2Points, round);
        gameState = GameStates.ROUNDOVER;

        StartCoroutine(StartNextRound());
    }

    [PunRPC]
    public void RPC_StartCountdown()
    {
        countdownTimer.gameObject.SetActive(true);
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

        Debug.Log("Game Ended");
        if (PhotonNetwork.IsMasterClient)
        {
            masterClient.ShowWinner(winningTeam);
        }
    }

    public void Surrender()
    {
        if (gameState == GameStates.POSTGAME)
            return;

        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("team", out object team))
        {
            PV.RPC(nameof(RPC_Surrender), RpcTarget.All, int.Parse(team.ToString()));
        }
    }

    [PunRPC]
    void RPC_Surrender(int surrenderingTeam)
    {
        gameState = GameStates.POSTGAME;

        roundTimer.SetActive(false);
        countdownTimer.SetActive(false);

        if (surrenderingTeam == 1)
        {
            team1Points = -1;
            winningTeam = 2; 
        }
        else
        {
            team2Points = -1;
            winningTeam = 1;
        }
        scoreboard.UpdateScores(team1Points, team2Points, round);
        if (PhotonNetwork.IsMasterClient)
        {
            masterClient.ShowWinner(winningTeam);
        }
    }
}
