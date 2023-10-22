using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    public static CameraController instance;

    [Header("References")]
    public Transform orientation;
    public Transform player;
    public Transform playerObj;
    public Rigidbody rb;

    public float rotationSpeed;

    public Transform combatLookAt;

    public GameObject thirdPersonCam;
    public GameObject combatCam;

    public GameObject playerDirectional;
    public GameObject enemyDirectional;

    public Transform Enemy;
    public CameraStyle currentStyle;

    private Animator animator;
    bool combatMode = false;
    public bool CombatMode { get { return combatMode; } private set { CombatMode = value; } }

    public enum CameraStyle
    {
        Basic,
        Combat,
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (!instance)
            instance = this;
        else
            Destroy(this);

        //playerDirectional = MouseController.instance.GetPlayerDirectional();
        animator = player.gameObject.GetComponent<Animator>();
    }

    private void Awake()
    {
        
    }
    private void Update()
    {
        // switch styles
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            combatMode = !combatMode;
            PlayerController.instance.ChangeSpeed();
            if (!combatMode)
            {
                SwitchCameraStyle(CameraStyle.Basic);
                var heading = Mathf.Atan2(orientation.right.z, orientation.right.x) * Mathf.Rad2Deg;
                thirdPersonCam.GetComponent<CinemachineFreeLook>().m_XAxis.Value = -heading;
                thirdPersonCam.GetComponent<CinemachineFreeLook>().m_YAxis.Value = 0.6f;
                animator.SetFloat("Xaxis", 0, 0, Time.deltaTime);
                animator.SetFloat("Yaxis", 0, 0, Time.deltaTime);
                animator.SetBool("CombatStance", false);
                MouseController.instance.ShowDirectionals(false);
            }
            else
            {
                SwitchCameraStyle(CameraStyle.Combat);
                MouseController.instance.ShowDirectionals(true);
                MouseController.instance.ResetCursor();
                animator.SetBool("CombatStance", true);
                playerDirectional.transform.position = Camera.main.WorldToScreenPoint(player.position);
            }
        }

        // rotate orientation
        Vector3 viewDir = player.position - new Vector3(transform.position.x, player.position.y, transform.position.z);
        orientation.forward = viewDir.normalized;

        // roate player object
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
                Vector3 dirToCombatLookAt = Enemy.position - new Vector3(player.position.x, Enemy.position.y, player.position.z);
                orientation.forward = dirToCombatLookAt.normalized;

                playerObj.forward = dirToCombatLookAt.normalized;
                combatLookAt.position = player.transform.position + dirToCombatLookAt.normalized * Mathf.Min(Vector3.Distance(Enemy.position, player.position), 4);
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
}
