using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Score : MonoBehaviour
{
    public TMP_Text round;
    public TMP_Text timer;
    public TMP_Text team1;
    public TMP_Text team2;

    public void UpdateScores(int _team1, int _team2, int _round)
    {
        round.text = "Round " + _round;
        team1.text = _team1.ToString();
        team2.text = _team2.ToString();
    }
}
