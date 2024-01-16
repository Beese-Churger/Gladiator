using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;
using UnityEngine.UI;
using ExitGames.Client.Photon;

public class Launcher : MonoBehaviourPunCallbacks
{
    public static Launcher instance;

    [SerializeField] TMP_InputField roomNameInputField;
    [SerializeField] TMP_Text roomName;
    [SerializeField] TMP_Text roomCode;
    [SerializeField] TMP_Text errorText;
    [SerializeField] Transform roomListContent;
    [SerializeField] Transform playerListContent;
    [SerializeField] GameObject roomListItemPrefab;
    [SerializeField] GameObject playerListItemPrefab;
    [SerializeField] GameObject startGameButton;


    [Header("CreateRoom")]
    [SerializeField] Toggle isPrivate;
    

    [Header("JoinRoom")]
    [SerializeField] TMP_InputField roomCodeInputField;
    List<RoomInfo> cachedRoomList = new();

    [Header("JoinedRoom")]
    //[SerializeField] TMP_Text player1;
    //[SerializeField] TMP_Text player2;
    [SerializeField] Transform player1pos;
    [SerializeField] Transform player2pos;
    [SerializeField] GameObject gladItemPrefab;
    //[SerializeField] Transform p1, p2;
    
    private void Awake()
    {
        if (!instance)
            instance = this;
        else
            Destroy(this);
    }
    void Start()
    {
        Debug.Log("Connecting to Master");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master");
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnJoinedLobby()
    {
        MenuManager.instance.OpenMenu("title");
        Debug.Log("Joined Lobby");
        PhotonNetwork.NickName = "Player " + Random.Range(0, 1000).ToString("0000");
    }

    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(roomNameInputField.text))
        {
            return;
        }
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 2,
            IsVisible = !isPrivate.isOn,

            CustomRoomProperties = new ExitGames.Client.Photon.Hashtable()
            {
                { "IsPrivate", isPrivate.isOn },
                { "RoomName", roomNameInputField.text } // Set the room code here
            },
            CustomRoomPropertiesForLobby = new string[] { "IsPrivate", "RoomName" }
        };
        PhotonNetwork.CreateRoom(GenerateRoomCode(4), roomOptions);
        MenuManager.instance.OpenMenu("loading");
    }

    string GenerateRoomCode(int length)
    {
        const string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        char[] code = new char[length];
        System.Random random = new System.Random();

        for (int i = 0; i < length; i++)
        {
            code[i] = characters[random.Next(characters.Length)];
        }

        return new string(code);
    }
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        errorText.text = "Room Creation Failed" + message;
        MenuManager.instance.OpenMenu("error");
    }
    public void JoinRoom(RoomInfo info)
    {
        PhotonNetwork.JoinRoom(info.Name);
        MenuManager.instance.OpenMenu("loading");
    }

    public void JoinRoomByCode()
    {
        if(string.IsNullOrEmpty(roomCodeInputField.text))
        {
            return;
        }
        // Search for a room with the specified name (room code)
        //RoomInfo[] rooms = roomlist;

        PhotonNetwork.JoinRoom(roomCodeInputField.text);

    }
    public override void OnJoinedRoom()
    {
        MenuManager.instance.OpenMenu("room");
        roomName.text = PhotonNetwork.CurrentRoom.CustomProperties["RoomName"].ToString();
        roomCode.text = PhotonNetwork.CurrentRoom.Name;

        Player[] players = PhotonNetwork.PlayerList;

        foreach(Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }
        for (int i = 0; i < players.Length; ++i)
        {
            if(players[i].IsMasterClient)
            {
                //Instantiate(playerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(players[i]);
                Instantiate(gladItemPrefab, player1pos.position, player1pos.rotation, player1pos.transform).GetComponent<PlayerListItem>().SetUp(players[i]);
            }
            else
            { 
                //Instantiate(playerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(players[i]);
                Instantiate(gladItemPrefab, player2pos.position, player2pos.rotation, player1pos.transform).GetComponent<PlayerListItem>().SetUp(players[i]);
            }
            
        }

    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Player[] players = PhotonNetwork.PlayerList;
        if(players.Length >= 2)
            startGameButton.SetActive(PhotonNetwork.IsMasterClient);
    }
    public void StartGame()
    {
        PhotonNetwork.LoadLevel(1);
    }
    public void LeaveRoom()
    {
        startGameButton.SetActive(false);
        PhotonNetwork.LeaveRoom();
        MenuManager.instance.OpenMenu("loading");
    }

    public override void OnLeftRoom()
    {
        Debug.Log("left");
        MenuManager.instance.OpenMenu("title");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        //cachedRoomList.Clear();
        foreach (Transform trans in roomListContent)
        {
            Destroy(trans.gameObject);
        }
        for(int i = 0; i < roomList.Count; ++i)
        {
            if (roomList[i].RemovedFromList)
                continue;
            Instantiate(roomListItemPrefab, roomListContent).GetComponent<RoomListItem>().SetUp(roomList[i]);
            //cachedRoomList.Add(roomList[i]);
        }
        
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (newPlayer.IsMasterClient)
        {
            //Instantiate(playerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(players[i]);
            Instantiate(gladItemPrefab, player1pos.position, player1pos.rotation).GetComponent<PlayerListItem>().SetUp(newPlayer);
        }
        else
        {
            //Instantiate(playerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(players[i]);
            Instantiate(gladItemPrefab, player2pos.position, player2pos.rotation).GetComponent<PlayerListItem>().SetUp(newPlayer);
        }
        Player[] players = PhotonNetwork.PlayerList;
        if (players.Length >= 2)
            startGameButton.SetActive(PhotonNetwork.IsMasterClient);
    }
}
