using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using UnityEngine.SceneManagement;
public class PostGame : MonoBehaviourPunCallbacks
{
    [SerializeField] GameManager gameManager;
    [SerializeField] TMP_Text teamWon;
    [SerializeField] CanvasGroup canvasGroup;

    string[] teams = new string[] { "BLUE", "ORANGE", "DRAW" };
    void Start()
    {
        canvasGroup.alpha = 0;
        gameManager = FindObjectOfType<GameManager>();
        //gameObject.SetActive(false);
    }

    public void GetOutOfThisRoomNow()
    {
        PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene(0);
    }

    public override void OnLeftRoom()
    {
        Debug.Log("left");
        SceneManager.LoadScene(0);
    }

    public void Show(int team)
    {
        Debug.Log(team);
        if (team != 3)
            teamWon.text = string.Format("{0} TEAM WINS", teams[team - 1]);
        else
            teamWon.text = teams[team - 1];
        canvasGroup.alpha = 1;
    }

    //public void Disable()
    //{
    //    gameObject.SetActive(false);
    //}
}