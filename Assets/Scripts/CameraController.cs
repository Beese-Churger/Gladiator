using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Photon.Pun;
public class CameraController : MonoBehaviour
{
    //public static CameraController instance;

    [Header("References")]
    MouseController mouseController;
    public Transform orientation;
    public Transform player;
    public Transform playerObj;
    public Rigidbody rb;
    PlayerController playerController;
    public float rotationSpeed;

    public Transform combatLookAt;
    public Vector3 orientationInitialFwd;

    public GameObject thirdPersonCam;
    public GameObject combatCam;

    public GameObject playerDirectional;
    public GameObject enemyDirectional;

    public Transform currentLock;
    public CameraStyle currentStyle;

    private Animator animator;

    [SerializeField] Transform canvas;
    [SerializeField] GameObject directionalUIprefab;
    public List<GameObject> indicators = new();

    bool combatMode = false;
    public bool CombatMode { get { return combatMode; } private set { CombatMode = value; } }

    PhotonView PV;
    public enum CameraStyle
    {
        Basic,
        Combat,
    }

    private void Start()
    {
        //if (!instance)
        //    instance = this;
        //else
        //    Destroy(this);

        //playerDirectional = MouseController.instance.GetPlayerDirectional();
    }

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        animator = player.gameObject.GetComponent<Animator>();
        playerController = player.gameObject.GetComponent<PlayerController>();
        mouseController = player.gameObject.GetComponent<MouseController>();

        PV = GetComponent<PhotonView>();
    }
    private void Update()
    {
        if (!PV.IsMine)
            return;

        
        // switch styles
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            combatMode = !combatMode;
            playerController.ChangeSpeed();

            if (!combatMode)
            {
                currentLock = null;
                SwitchCameraStyle(CameraStyle.Basic);
                TurnOffIndicators();
                var heading = Mathf.Atan2(orientation.right.z, orientation.right.x) * Mathf.Rad2Deg;
                thirdPersonCam.GetComponent<CinemachineFreeLook>().m_XAxis.Value = -heading;
                thirdPersonCam.GetComponent<CinemachineFreeLook>().m_YAxis.Value = 0.6f;
                animator.SetFloat("Xaxis", 0, 0, Time.deltaTime);
                animator.SetFloat("Yaxis", 0, 0, Time.deltaTime);
                animator.SetBool("CombatStance", false);
                mouseController.ShowDirectionals(false);
            }
            else
            {
                playerController.LockOntoOpponent();
                TurnOnIndicators();
                SwitchCameraStyle(CameraStyle.Combat);
                mouseController.ShowDirectionals(true);
                mouseController.ResetCursor();
                animator.SetBool("CombatStance", true);
                playerDirectional.transform.position = Camera.main.WorldToScreenPoint(player.position);
            }
        }

        // rotate orientation
        Vector3 viewDir = player.position - new Vector3(Camera.main.transform.position.x, player.position.y, Camera.main.transform.position.z);
        orientation.forward = viewDir.normalized;

        // rotate player object
        switch(currentStyle)
        {
            case CameraStyle.Basic:
            {
                float horizontalInput = Input.GetAxis("Horizontal");
                float verticalInput = Input.GetAxis("Vertical");
                Vector3 inputDir = orientation.forward * verticalInput + orientation.right * horizontalInput;

                if (inputDir != Vector3.zero)
                    playerObj.forward = Vector3.Slerp(playerObj.forward, inputDir.normalized, Time.deltaTime * rotationSpeed);
                break;
            }
            case CameraStyle.Combat:
            {
                    
                if (!currentLock)
                {
                    currentLock = combatLookAt;
                }

                if(playerController.opponentsInFOV.Count <= 0)
                {
                    combatLookAt.position = playerObj.position + (orientationInitialFwd * 4);
                }
                else if(playerController.opponentsInFOV.Count == 1)
                {
                    SwapTarget();
                }

                if (Input.GetMouseButtonDown(2))
                {
                    SwapTarget();
                }
                
                Vector3 dirToCombatLookAt = currentLock.position - new Vector3(player.position.x, currentLock.position.y, player.position.z);
                orientation.forward = dirToCombatLookAt.normalized;

                playerObj.forward = dirToCombatLookAt.normalized;

                combatLookAt.position = player.transform.position + dirToCombatLookAt.normalized * Mathf.Min(Vector3.Distance(currentLock.position, player.position), 2);
                combatLookAt.rotation = playerObj.rotation;

                playerDirectional.transform.position = Vector2.Lerp(playerDirectional.transform.position,Camera.main.WorldToScreenPoint(player.position), 10 * Time.deltaTime);
                break;
            }
        }
    }

    private void SwitchCameraStyle(CameraStyle newStyle)
    {
        combatCam.SetActive(false);
        thirdPersonCam.SetActive(false);

        if (newStyle == CameraStyle.Basic) thirdPersonCam.SetActive(true);
        if (newStyle == CameraStyle.Combat) combatCam.SetActive(true);

        currentStyle = newStyle;
    }

    public void SwapTarget()
    {
        if (playerController.opponentsInFOV.Count > 0)
        {
            int currIndex = playerController.opponentsInFOV.IndexOf(currentLock.gameObject);
            if (currIndex == playerController.opponentsInFOV.Count - 1)
                currIndex = 0;
            else
                currIndex++;

            currentLock = playerController.opponentsInFOV[currIndex].transform;
        }
    }
    private void TurnOnIndicators()
    {
        if (indicators.Count <= 0)
            return;

        for(int i = 0; i < indicators.Count; ++i)
        {
            //if (!playerController.opponentsInFOV.Contains(indicators[i].GetComponent<Indicator>().GetTarget()))
            //{
            //    indicators[i].SetActive(false);
            //}
            //else
            //{
            //    indicators[i].SetActive(true);
            //}
            indicators[i].SetActive(true);
        }
    }

    private void TurnOffIndicators()
    {
        if (indicators.Count <= 0)
            return;

        for (int i = 0; i < indicators.Count; ++i)
        {
            indicators[i].SetActive(false);
        }
    }
}
