using UnityEngine;
using ExitGames.Client.Photon;
using Photon.Realtime;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;
public class RoundTimer : MonoBehaviourPunCallbacks
{
    public delegate void RoundTimerHasExpired();

    public const string RoundStartTime = "RoundStartTime";

    [Header("Countdown time in seconds")]
    public float Countdown = 181.0f;

    private bool isTimerRunning;

    private int startTime;

    [Header("Reference to a Text component for visualizing the countdown")]
    public TMP_Text Text;


    /// <summary>
    ///     Called when the timer has expired.
    /// </summary>
    public static event RoundTimerHasExpired OnRoundTimerHasExpired;


    public void Start()
    {
        if (this.Text == null) Debug.LogError("Reference to 'Text' is not set. Please set a valid reference.", this);
    }

    public override void OnEnable()
    {
        Debug.Log("OnEnable roundTimer");
        base.OnEnable();

        // the starttime may already be in the props. look it up.
        Initialize();
    }

    public override void OnDisable()
    {
        base.OnDisable();
        Debug.Log("OnDisable roundTimer");
    }


    public void Update()
    {
        if (!this.isTimerRunning) return;

        float countdown = TimeRemaining();
        this.Text.text = formatTimer(countdown);

        if (countdown > 0.0f) return;

        OnTimerEnds();
    }


    private void OnTimerRuns()
    {
        this.enabled = true;
        this.isTimerRunning = true;
    }

    public void OnTimerEnds()
    {
        this.isTimerRunning = false;
        this.enabled = false;

        Debug.Log("Emptying info round text.", this.Text);
        this.Text.text = "00:00";

        if (OnRoundTimerHasExpired != null && TimeRemaining() <= 0) OnRoundTimerHasExpired();
    }


    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        Debug.Log("CountdownTimer.OnRoomPropertiesUpdate " + propertiesThatChanged.ToStringFull());
        Initialize();
    }


    public void Initialize()
    {
        int propStartTime;
        if (TryGetStartTime(out propStartTime))
        {
            this.startTime = propStartTime;
            Debug.Log("Initialize sets StartTime " + this.startTime + " server time now: " + PhotonNetwork.ServerTimestamp + " remain: " + TimeRemaining());


            this.isTimerRunning = TimeRemaining() > 0;

            if (this.isTimerRunning)
            {
                Debug.Log("roundtimer");
                OnTimerRuns();
            }
            else
                OnTimerEnds();
        }
    }


    private float TimeRemaining()
    {
        int timer = PhotonNetwork.ServerTimestamp - this.startTime;
        return this.Countdown - timer / 1000f;
    }


    public static bool TryGetStartTime(out int startTimestamp)
    {
        startTimestamp = PhotonNetwork.ServerTimestamp;

        object startTimeFromProps;
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(RoundStartTime, out startTimeFromProps))
        {
            startTimestamp = (int)startTimeFromProps;
            return true;
        }

        return false;
    }


    public static void SetStartTime()
    {
        int startTime = 0;
        //bool wasSet = TryGetStartTime(out startTime);

        Hashtable props = new Hashtable
            {
                {RoundTimer.RoundStartTime, (int)PhotonNetwork.ServerTimestamp}
            };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);


        Debug.Log("Set Custom Props for Time: " + props.ToStringFull() + " wasSet: " /*+ wasSet*/);
    }

    public string formatTimer(float currentTime)
    {
        float minutes = Mathf.FloorToInt(currentTime / 60);
        float seconds = Mathf.FloorToInt(currentTime % 60);

        return string.Format("{00:00}:{01:00}", minutes, seconds);
    }
}
