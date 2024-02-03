using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class ItemCycle : MonoBehaviourPunCallbacks
{
    public List<string> itemList = new(); // List of items
    private int currentIndex = 0; // Index of the currently displayed item
    public Player player;
    public GameObject left, right;
    public TMP_Text itemDisplay;
    // Start is called before the first frame update
    void init()
    {
        itemList.Add("Trident");
        itemList.Add("Short Sword");
        // Ensure there's at least one item in the list
        if (itemList.Count == 0)
        {
            Debug.LogError("Item list is empty!");
            return;
        }

        // Display the first item initially
        DisplayCurrentItem();

        if (player != PhotonNetwork.LocalPlayer)
        {
            left.SetActive(false);
            right.SetActive(false);
        }
    }

    public void SetUp(Player player)
    {
        this.player = player;
        init();
    }
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            object playerWeapon;

            if (p.CustomProperties.TryGetValue(GladiatorInfo.PLAYER_LOADED_LEVEL, out playerWeapon))
            {
                if (!p.Equals(PhotonNetwork.LocalPlayer))
                {

                    itemDisplay.text = playerWeapon.ToString();
                }
            }
        }
    }

    // Cycle through the items based on the provided direction
    public void CycleItems(int direction)
    {
        currentIndex += direction;

        // Wrap around if reaching the end of the list
        if (currentIndex < 0)
        {
            currentIndex = itemList.Count - 1;
        }
        else if (currentIndex >= itemList.Count)
        {
            currentIndex = 0;
        }

        // Display the current item
        DisplayCurrentItem();
    }


    // Display the current item
    void DisplayCurrentItem()
    {
        itemDisplay.text = itemList[currentIndex];
        UpdatePlayerProperties(currentIndex);
    }

    void UpdatePlayerProperties(int newItem)
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer != null)
        {
            ExitGames.Client.Photon.Hashtable props = new()
            {
                { GladiatorInfo.PLAYER_WEAPON, newItem.ToString() }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
    }

    // Called when a player's custom properties are changed
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        // Check if the player's custom properties contain the key we are interested in
        if (changedProps.ContainsKey(GladiatorInfo.PLAYER_WEAPON))
        {
            if (targetPlayer == player)
            {
                itemDisplay.text = itemList[int.Parse(changedProps[GladiatorInfo.PLAYER_WEAPON].ToString())];
                Debug.Log(changedProps[GladiatorInfo.PLAYER_WEAPON]);
            }
        }
    }
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (!this)
            return;

        if (player == otherPlayer)
        {
            Destroy(gameObject);
        }
        if (player.IsMasterClient)
        {
            Transform parent = GameObject.Find("Team1").transform;
            transform.SetParent(parent);
            transform.position = new Vector3(parent.position.x, transform.position.y, parent.position.z);
        }
    }

    private void OnDisable()
    {
        Destroy(gameObject);
    }
}
