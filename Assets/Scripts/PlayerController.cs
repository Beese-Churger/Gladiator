using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class PlayerController : MonoBehaviourPunCallbacks, IDamageable
{
    public enum PlayerStates
    {
        IDLE,
        COMBAT,
        ATTACKING,
        HIT
    }

    //public static PlayerController instance;
    [SerializeField] PlayerManager playerManager;
    [Header("Stats")]
    public GameObject healthbar;
    public Slider healthBarFill;
    public Slider staminaBarFill;

    public float maxHealth = 100f;
    private float currentHealth;

    public float maxStamina = 100f;
    private float currentStamina;

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

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    public Transform orientation;
    [SerializeField] Transform model;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;
    Rigidbody rb;
    Vector2 speedPercent;

    [Header("CombatStuff")]
    [SerializeField] Detect detectionRadius;
    public List<GameObject> opponentsInFOV = new();
    [SerializeField] Collider rHand, lHand;

    public float lightHitboxActivationTime = 0.3f;
    public float lightHitboxDeactivationTime = 0.7f;

    public List<float> lightStaminaCost = new() { 8f, 6f, 6f };
    public bool isAttacking = false;
    float lastAttack;
    float regenDelay = 1f;
    float staminaRegenAmount = 20f;
    bool canRegenStamina = true;
    bool hasHyperArmor = false;
    bool tookHit = false;
    bool isCombo = false;

    PhotonView PV;
    public Animator animator;
    private void Start()
    {
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
        playerManager = FindObjectOfType<PlayerManager>();

        rb.freezeRotation = true;
        readyToJump = true;
        moveSpeed = freeSpeed;

        currentHealth = maxHealth;
        currentStamina = maxStamina;
        currDir = MouseController.DirectionalInput.TOP;
        lastAttack = Time.time;
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

        // hit taken
        if (!isAttacking)
        {
            if (tookHit == true)
            {
                tookHit = false;
                animator.SetTrigger("HIT");
            }
        }

        // ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, whatIsGround);

        // handle drag
        if (grounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0;

        if (!PV.IsMine)
            return;

        CheckWhoCanLock();
        UpdateUI();
        MyInput();
        SpeedControl();

        if(cameraController.CombatMode)
        {
            if (Input.GetMouseButtonDown(0) && AbleToMove() && !isAttacking)
            {
                //Debug.Log("Light" + MouseController.instance.GetInputDirection().ToString());
                LightAttack();
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
        if(canRegenStamina && lastAttack + regenDelay < Time.time)
        {
            RegenStamina(staminaRegenAmount);
        }
        if (!PV.IsMine)
        {
            orientation.rotation = model.rotation;
            return;
        }


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

    public void LightAttack()
    {
        isAttacking = true;
        PV.RPC(nameof(RPC_LightAttack), RpcTarget.All, mouseController.GetInputDirection());
    }
    [PunRPC]
    public void RPC_LightAttack(MouseController.DirectionalInput direction)
    {
        isAttacking = true;
        animator.SetTrigger("LIGHT");
        animator.SetTrigger(direction.ToString());

        // choose collider to activate
        Collider collider;
        float staminaCost = 0f;
        switch(direction)
        {
            case MouseController.DirectionalInput.TOP:
                collider = rHand;
                staminaCost = lightStaminaCost[0];
                break;
            case MouseController.DirectionalInput.LEFT:
                collider = lHand;
                staminaCost = lightStaminaCost[1];
                break;
            case MouseController.DirectionalInput.RIGHT:
                collider = rHand;
                staminaCost = lightStaminaCost[2];
                break;
            default:
                collider = rHand;
                break;
        }
        
        UseStaminaAttack(staminaCost);
        // Schedule hitbox activation and deactivation using animation events
        StartCoroutine(PerformLightAttack(collider));
    } 

    IEnumerator PerformLightAttack(Collider collider)
    {

        if(ShouldInterruptAttack())
        {
            isAttacking = false;
            lastAttack = Time.time;
            animator.SetTrigger("HIT");
            yield break;
        }

        //UpdateAttackIndicator();
        yield return null; // yield 1 frame to ensure animation starts;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        yield return new WaitForSeconds(lightHitboxActivationTime);

        collider.enabled = true;

        yield return new WaitForSeconds(lightHitboxDeactivationTime - lightHitboxActivationTime);

        collider.enabled = false;

        yield return new WaitForSeconds(0.1f);
        isAttacking = false;
        lastAttack = Time.time;
        // when animation ends disable set isattacking to false
        //yield return new WaitForSeconds(stateInfo.length * 0.8f);

        //isAttacking = false;

    }

    bool ShouldInterruptAttack()
    {
        return tookHit && !hasHyperArmor;
    }

    //public void UpdateAttackIndicator(GameObject theAttacker)
    //{

    //}
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
        if (isAttacking)
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

    private void UpdateStaminaBar()
    {
        staminaBarFill.value = currentStamina / maxStamina;
    }

    public void CheckIfBlocked(PlayerController enemy, MouseController.DirectionalInput enemyDir, int damage)
    {
        //check if player is facing enemy
        Vector3 directionToPlayer = transform.position - enemy.transform.position;
        float dotProduct = Vector3.Dot(orientation.forward, directionToPlayer.normalized);

        MouseController.DirectionalInput incomingDir = enemyDir;
        if(dotProduct < 0)
        {
            switch(enemyDir)
            {
                case MouseController.DirectionalInput.LEFT:
                    incomingDir = MouseController.DirectionalInput.RIGHT;
                    break;
                case MouseController.DirectionalInput.RIGHT:
                    incomingDir = MouseController.DirectionalInput.LEFT;
                    break;
                default:
                    break;
            }
        }
        if (currDir == incomingDir)
        {
            animator.SetTrigger("BLOCK");
        }
        else
            TakeDamage(damage);
    }
    public void TakeDamage(float damage)
    {
        PV.RPC(nameof(RPC_TakeDamage), RpcTarget.All, damage);
    }

    [PunRPC]
    void RPC_TakeDamage(float damage, PhotonMessageInfo info)
    {
        tookHit = true;

        currentHealth -= damage;
        UpdateHealthBar();

        //Debug.Log($"Hit by {info.Sender}");

        if (currentHealth <= 0)
        {
            Die();
            PlayerManager.Find(info.Sender).GetKill();
        }
    }

    public void UseStaminaAttack(float amount)
    {
        lastAttack = Time.time;
        currentStamina -= amount;
        currentStamina = Mathf.Clamp(currentStamina, 0f, 100f);
        UpdateStaminaBar();
    }

    public void UseStamina(float amount)
    {
        PV.RPC(nameof(RPC_UseStamina), RpcTarget.All, amount);
    }
    [PunRPC]
    void RPC_UseStamina(float amount)
    {
        currentStamina -= amount;

        UpdateStaminaBar();
    }

    public void RegenStamina(float amount)
    {
        if(currentStamina < maxStamina)
            currentStamina += amount * Time.fixedDeltaTime;
        currentStamina = Mathf.Clamp(currentStamina, 0f, 100f);
        UpdateStaminaBar();
    }

    void Die()
    {
        playerManager.Die();
    }
}
