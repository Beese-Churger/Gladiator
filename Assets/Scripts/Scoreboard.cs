using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
public class Scoreboard : MonoBehaviourPunCallbacks
{
	[SerializeField] Transform team1Container;
	[SerializeField] Transform team2Container;
	[SerializeField] GameObject scoreboardItemPrefab;
	[SerializeField] CanvasGroup canvasGroup;

	Dictionary<Player, ScoreboardItem> scoreboardItems = new Dictionary<Player, ScoreboardItem>();

	PhotonView PV;
	void Start()
	{
		canvasGroup.alpha = 0;
		//foreach (Player player in PhotonNetwork.PlayerList)
		//{
		//	AddScoreboardItem(player);
		//}
		StartCoroutine(WaitToGetPlayers());
	}
	IEnumerator WaitToGetPlayers()
	{
		yield return new WaitForSeconds(1f);

		foreach (Player player in PhotonNetwork.PlayerList)
		{
			AddScoreboardItem(player);
		}
	}
	public override void OnPlayerEnteredRoom(Player newPlayer)
	{
		AddScoreboardItem(newPlayer);
	}

	public override void OnPlayerLeftRoom(Player otherPlayer)
	{
		RemoveScoreboardItem(otherPlayer);
	}


	void AddScoreboardItem(Player player)
	{
		if (player.CustomProperties.TryGetValue("team", out object team))
		{
			if(team.ToString() == "1")
            {
				ScoreboardItem item = Instantiate(scoreboardItemPrefab, team1Container).GetComponent<ScoreboardItem>();
				item.Initialize(player);
				scoreboardItems[player] = item;
			}
			else
            {
				ScoreboardItem item = Instantiate(scoreboardItemPrefab, team2Container).GetComponent<ScoreboardItem>();
				item.Initialize(player);
				scoreboardItems[player] = item;
			}
		}
	}

	void RemoveScoreboardItem(Player player)
	{
		Destroy(scoreboardItems[player].gameObject);
		scoreboardItems.Remove(player);
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Tab))
		{
			canvasGroup.alpha = 1;
		}
		else if (Input.GetKeyUp(KeyCode.Tab))
		{
			canvasGroup.alpha = 0;
		}
	}
}
