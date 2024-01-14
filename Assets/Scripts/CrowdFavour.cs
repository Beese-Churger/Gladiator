using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CrowdFavour : MonoBehaviour
{
    public static CrowdFavour Instance;
    public Slider advantageSlider;
    public Text team1Text;
    public Text team2Text;

    public int team1Favour = 0;
    public int team2Favour = 0;

    private void Start()
    {
        Instance = this;
        UpdateSlider();
    }

    public void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            IncreaseTeam1Favour();
        }
        if(Input.GetKeyDown(KeyCode.Alpha2))
        {
            IncreaseTeam2Favour();
        }
    }
    public void IncreaseTeam1Favour()
    {
        team1Favour++;
        UpdateSlider();
    }

    public void DecreaseTeam1Favour()
    {
        team1Favour--;
        UpdateSlider();
    }

    public void IncreaseTeam2Favour()
    {
        team2Favour++;
        UpdateSlider();
    }

    public void DecreaseTeam2Favour()
    {
        team2Favour--;
        UpdateSlider();
    }

    private void UpdateSlider()
    {
        advantageSlider.maxValue = Mathf.Max(team1Favour, team2Favour) + 1;
        advantageSlider.minValue = Mathf.Min(team1Favour, team2Favour) - 1;

        advantageSlider.value = team1Favour;

        //team1Text.text = "Team 1: " + team1Favour;
        //team2Text.text = "Team 2: " + team2Favour;
    }
}
