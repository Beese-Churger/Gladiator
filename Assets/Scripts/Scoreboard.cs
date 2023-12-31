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
    public TMP_Text round;
    public TMP_Text timer;
    public TMP_Text team1;
    public TMP_Text team2;

	[SerializeField] Transform team1Container;
	[SerializeField] Transform team2Container;
	[SerializeField] GameObject scoreboardItemPrefab;
	[SerializeField] CanvasGroup canvasGroup;

	Dictionary<Player, ScoreboardItem> scoreboardItems = new Dictionary<Player, ScoreboardItem>();

	void Start()
	{
		canvasGroup.alpha = 0;
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

	public void updateScores(int _team1, int _team2, int _round)
    {
		round.text = "Round " + _round;
		team1.text = _team1.ToString();
		team2.text = _team2.ToString();
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
