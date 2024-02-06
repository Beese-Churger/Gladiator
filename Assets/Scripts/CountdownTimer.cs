using UnityEngine;
using ExitGames.Client.Photon;
using Photon.Realtime;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;
public class CountdownTimer : MonoBehaviourPunCallbacks
{
    public delegate void CountdownTimerHasExpired();

    public const string CountdownStartTime = "StartTime";

    [Header("Countdown time in seconds")]
    public float Countdown = 5.0f;

    private bool isTimerRunning;

    private int startTime;

    [Header("Reference to a Text component for visualizing the countdown")]
    public TMP_Text Text;

    public string timering = " ";
    /// <summary>
    ///     Called when the timer has expired.
    /// </summary>
    public static event CountdownTimerHasExpired OnCountdownTimerHasExpired;


    public void Start()
    {
        if (this.Text == null) Debug.LogError("Reference to 'Text' is not set. Please set a valid reference.", this);
    }

    public override void OnEnable()
    {
        Debug.Log("OnEnable CountdownTimer");
        base.OnEnable();

        // the starttime may already be in the props. look it up.
        Initialize();
    }

    public override void OnDisable()
    {
        base.OnDisable();
        Debug.Log("OnDisable CountdownTimer");
    }


    public void Update()
    {
        if (!this.isTimerRunning) return;

        float countdown = TimeRemaining();

        this.Text.text = string.Format("{0}", countdown.ToString("n0"));
        
        if(timering != Text.text)
        {
            if(Text.text == "0")
            {
                AudioManager.Instance.PlaySFX("Start");
            }
            else
                AudioManager.Instance.PlaySFX("Drum");
        }
        if (countdown > 0.0f) return;

        OnTimerEnds();
    }
    public void LateUpdate()
    {
        timering = Text.text;
    }

    public void OnTimerRuns()
    {
        this.isTimerRunning = true;
        this.enabled = true;
    }

    public void OnTimerEnds()
    {
        this.isTimerRunning = false;
        this.enabled = false;
       

        Debug.Log("Emptying info text.", this.Text);
        this.Text.text = string.Empty;

        if (OnCountdownTimerHasExpired != null && TimeRemaining() <= 0)
            OnCountdownTimerHasExpired();

        Debug.Log("hit");
        //gameObject.SetActive(false);
    }


    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        Debug.Log("RoundTimer.OnRoomPropertiesUpdate " + propertiesThatChanged.ToStringFull());
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
                OnTimerRuns();
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
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(CountdownStartTime, out startTimeFromProps))
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
                {CountdownTimer.CountdownStartTime, (int)PhotonNetwork.ServerTimestamp}
            };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        Debug.Log("Set Custom Props for Time: " + props.ToStringFull() + " wasSet: " /*+ wasSet*/);
    }
}
