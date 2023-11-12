using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class PlayerController : MonoBehaviourPunCallbacks, IDamageable
{
    //public static PlayerController instance;
    [SerializeField] PlayerManager playerManager;
    [Header("Stats")]
    public GameObject healthbar;
    public Slider healthBarFill;
    public Slider staminaBarFill;

    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Movement")]
    public float moveSpeed;
    public float combatSpeed;
    public float freeSpeed;

    public float groundDrag;

    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;

    [HideInInspector] public float walkSpeed;
    [HideInInspector] public float sprintSpeed;

    [Header("Camera")]
    [SerializeField] GameObject cameraHolder;
    CameraController cameraController;
    MouseController mouseController;
    public MouseController.DirectionalInput currDir;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    public Transform orientation;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;
    Rigidbody rb;
    Vector2 speedPercent;

    [Header("CombatStuff")]
    [SerializeField] Detect detectionRadius;
    public List<GameObject> opponentsInFOV = new();

    PhotonView PV;
    public Animator animator;
    private void Start()
    {
        //if (!instance)
        //    instance = this;
        //else
        //    Destroy(this);
        
        if(!PV.IsMine)
        {
            foreach (Transform child in cameraHolder.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void Awake()
    {
        PV = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody>();

        mouseController = GetComponent<MouseController>();
        cameraController = cameraHolder.GetComponent<CameraController>();

        rb.freezeRotation = true;
        readyToJump = true;
        moveSpeed = freeSpeed;

        currentHealth = maxHealth;
        currDir = MouseController.DirectionalInput.TOP;
    }

    private void Update()
    {
        // animate player movement
        if (animator != null && animator.isActiveAndEnabled)
        {
            if (cameraController.CombatMode)
            {
                speedPercent = new Vector2(Mathf.Clamp(orientation.InverseTransformDirection(rb.velocity).x, -1f, 1f), Mathf.Clamp(orientation.InverseTransformDirection(rb.velocity).z, -1f, 1f));
                animator.SetFloat("Xaxis", speedPercent.x, 0.1f, Time.deltaTime);
                animator.SetFloat("Yaxis", speedPercent.y, 0.1f, Time.deltaTime);
            }
            else
            {
                speedPercent = new Vector2(Mathf.Abs(Mathf.Clamp(orientation.InverseTransformDirection(rb.velocity).x, -1f, 1f)), Mathf.Abs(Mathf.Clamp(orientation.InverseTransformDirection(rb.velocity).z, -1f, 1f)));
                animator.SetFloat("Yaxis", Mathf.Max(speedPercent.x, speedPercent.y), 0.1f, Time.deltaTime);
            }
        }

        // handle drag
        if (grounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0;

        if (!PV.IsMine)
            return;

        CheckWhoCanLock();
        UpdateUI();
        // ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, whatIsGround);

        MyInput();
        SpeedControl();

        if(cameraController.CombatMode)
        {
            //speedPercent = new Vector2(Mathf.Clamp(orientation.InverseTransformDirection(rb.velocity).x, -1f, 1f), Mathf.Clamp(orientation.InverseTransformDirection(rb.velocity).z, -1f, 1f));
            //animator.SetFloat("Xaxis", speedPercent.x, 0.1f, Time.deltaTime);
            //animator.SetFloat("Yaxis", speedPercent.y, 0.1f, Time.deltaTime);

            if (Input.GetMouseButtonDown(0) && AbleToMove())
            {
                //Debug.Log("Light" + MouseController.instance.GetInputDirection().ToString());
           
                animator.SetTrigger("LIGHT");
                animator.SetTrigger(mouseController.GetInputDirection().ToString());
            }
            if (Input.GetMouseButtonDown(1) && AbleToMove())
            {
                Debug.Log("Heavy" + mouseController.GetInputDirection().ToString());

                //animator.SetTrigger("LIGHT");
                //animator.SetTrigger(MouseController.instance.GetInputDirection().ToString());
            }

        }
        else
        {
            //speedPercent = new Vector2(Mathf.Abs(Mathf.Clamp(orientation.InverseTransformDirection(rb.velocity).x, -1f, 1f)), Mathf.Abs(Mathf.Clamp(orientation.InverseTransformDirection(rb.velocity).z, -1f, 1f)));
            ////animator.SetFloat("Xaxis", speedPercent.x, 0.1f, Time.deltaTime);
            //animator.SetFloat("Yaxis", Mathf.Max(speedPercent.x,speedPercent.y), 0.1f, Time.deltaTime);
        }



    }

    private void FixedUpdate()
    {
        if (!PV.IsMine)
            return;

        if (!AbleToMove())
            return;
        MovePlayer();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
    }

    private void MovePlayer()
    {
        // calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // on ground
        if (grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);

        // in air
        else if (!grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
    }

    public void LockOntoOpponent()
    {
        CheckWhoCanLock();
        if(!cameraController.currentLock)
        {
            if(opponentsInFOV.Count > 0)
                cameraController.currentLock = opponentsInFOV[0].transform;
        }
        
    }
    public void CheckWhoCanLock()
    {
        for(int i = 0; i < detectionRadius.opponentsInRange.Count; ++i)
        {
            if (!detectionRadius.opponentsInRange[i])
            {
                detectionRadius.opponentsInRange.Remove(detectionRadius.opponentsInRange[i]);
                continue;
            }


            if (IsInCameraFrustum(i))
            {
                if(!opponentsInFOV.Contains(detectionRadius.opponentsInRange[i]))
                    opponentsInFOV.Add(detectionRadius.opponentsInRange[i]);
            }

            else
            {
                //Debug.Log("L");
                opponentsInFOV.Remove(detectionRadius.opponentsInRange[i]);
            }

        }
    }
    private bool IsInCameraFrustum(int index)
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

        if (GeometryUtility.TestPlanesAABB(planes, detectionRadius.opponentsInRange[index].GetComponent<Collider>().bounds))
            return true; // Object is within the camera's frustum

        return false; // Object is not within the camera's frustum
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // limit velocity if needed
        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }

    private bool AbleToMove()
    {
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Movement") && !animator.GetCurrentAnimatorStateInfo(0).IsName("CombatMovement"))
            return false;
        return true;
    }
    public void ChangeSpeed()
    {
        if (cameraController.CombatMode)
        {
            moveSpeed = combatSpeed;
        }
        else
            moveSpeed = freeSpeed;
    }

    public void SetDir(MouseController.DirectionalInput _dir)
    {
        currDir = _dir;
        //Debug.Log("Hit");
    }

    public MouseController.DirectionalInput GetDir()
    {
        return currDir;
    }
    public void UpdateUI()
    {

        if (Input.GetKeyDown(KeyCode.N))
        {
            SetHealth(currentHealth - 10);
        }
    }

    public void SetHealth(float health)
    {
        currentHealth = health;
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        healthBarFill.value = currentHealth / maxHealth;
    }

    public void TakeDamage(float damage)
    {
        PV.RPC(nameof(RPC_TakeDamage), PV.Owner, damage);
    }

    [PunRPC]
    void RPC_TakeDamage(float damage, PhotonMessageInfo info)
    {
        currentHealth -= damage;

        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            Die();
            PlayerManager.Find(info.Sender).GetKill();
        }
    }

    void Die()
    {
        playerManager.Die();
    }


}
