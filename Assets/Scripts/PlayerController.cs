using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable, IDamageable
{
    public enum PlayerState
    {
        COMBAT,
        PARRIED,
        EXHAUSTED,
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
    [SerializeField] Collider playerCollider;
    [SerializeField] Detect detectionRadius;
    public List<GameObject> opponentsInFOV = new();
    public List<PlayerController> opponentsInAttackRange = new();
    [SerializeField] Collider rHand, lHand;
    float lastHitTime;
    float timeToMove = 0.05f;

    bool isDodging = false;
    bool dodgeLeft = false;
    bool isInvincible = false;
    public float iFrameDuration = 0.2f;
    float lastDodgeTime;
    float dodgeCD = 0.3f;
    public float lightHitboxActivationTime = 0.5f;
    public float lightHitboxDeactivationTime = 0.6f;

    public float heavyHitboxActivationTime = 0.8f;
    public float heavyHitboxDeactivationTime = 0.9f;

    public List<float> lightStaminaCost = new() { 8f, 6f, 6f };
    public List<float> heavyStaminaCost = new() { 18f, 12f, 12f };
    public bool isAttacking = false;
    public bool isHeavy = false;
    float lastAttack;
    float regenDelay = 1f;
    float staminaRegenAmount = 20f;
    bool canRegenStamina = true;
    bool hasHyperArmor = false;
    bool tookHit = false;
    bool isCombo = false;
    public bool canParry = false;
    bool canFeint = false;
    bool feint = false;
    bool parry = true;
    bool isParried = false;
    public int lockOnPlayerID = -1;

    bool isAttackBlocked = false;
    bool isAttackParried = false;
    int playerIDParried = -1;

    [Header("Lag Stuff")]
    //Values that will be synced over network
    Vector3 latestPos;
    Quaternion latestRot;
    //Lag compensation
    float currentTime = 0;
    double currentPacketTime = 0;
    double lastPacketTime = 0;
    Vector3 positionAtLastPacket = Vector3.zero;
    Quaternion rotationAtLastPacket = Quaternion.identity;


    Coroutine heavyAttack;
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
        lastDodgeTime = Time.time;
        lastHitTime = Time.time;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Writing to the network
            stream.SendNext(isAttackParried);
            // Serialize any additional data related to the parry action
        }
        else
        {
            // Reading from the network
            //if(PV.ViewID == playerIDParried)
                isAttackParried = (bool)stream.ReceiveNext();
            // Deserialize any additional data related to the parry action
        }
    }
    private void Update()
    {

        // animate player movement
        if (animator != null && animator.isActiveAndEnabled)
        {
            if (cameraController.CombatMode)
            {
                speedPercent = new Vector2(Mathf.Clamp(orientation.InverseTransformDirection(rb.velocity).x, -1f, 1f)
                                        , Mathf.Clamp(orientation.InverseTransformDirection(rb.velocity).z, -1f, 1f));

                animator.SetFloat("Xaxis", speedPercent.x, 0.1f, Time.deltaTime);
                animator.SetFloat("Yaxis", speedPercent.y, 0.1f, Time.deltaTime);
            }
            else
            {
                speedPercent = new Vector2(Mathf.Abs(Mathf.Clamp(orientation.InverseTransformDirection(rb.velocity).x, -1f, 1f))
                                        , Mathf.Abs(Mathf.Clamp(orientation.InverseTransformDirection(rb.velocity).z, -1f, 1f)));

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
            if (Input.GetMouseButtonDown(0) && AbleToMove())
            {
                LightAttack();
            }
            if (Input.GetMouseButtonDown(1) && AbleToMove())
            {
                bool hasParry = false;
                for(int i = 0; i < opponentsInAttackRange.Count; ++i)
                {
                    PlayerController enemyController = opponentsInAttackRange[i];
                    if (PV.ViewID == enemyController.lockOnPlayerID)
                    {
                        if(enemyController.canParry)
                        {
                            if(CheckIfCanParry(enemyController, enemyController.GetDir(), enemyController.isHeavy))
                            {
                                ParryAttack(enemyController.isHeavy);
                                hasParry = true;
                            }
                        }
                    }
                }
                if(!hasParry)   
                    HeavyAttack();
            }

            if(canFeint && Input.GetKeyDown(KeyCode.E))
            {
                Feint(true);
                canFeint = false;
            }
            if(lastDodgeTime + dodgeCD < Time.time && AbleToMove())
            {
                if (Input.GetKey(KeyCode.A) && Input.GetKeyDown(KeyCode.Space))
                {
                    Dodge(true);
                }
                else if (Input.GetKey(KeyCode.D) && Input.GetKeyDown(KeyCode.Space))
                {
                    Dodge(false);
                }
            }
        }
        else
        {

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
        if (isDodging)
        {
            MoveTowards(dodgeLeft);
        }

        if (!AbleToMove())
            return;

        MovePlayer();


    }

    private void MoveTowards(bool left)
    {
        if(left)
        {
            rb.AddForce(-orientation.right * 100f, ForceMode.Force);
        }
        else
        {
            rb.AddForce(orientation.right * 100f, ForceMode.Force);
        }

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
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }
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

        if(ShouldInterruptAction())
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

    public void HeavyAttack()
    {
        isAttacking = true;
        PV.RPC(nameof(RPC_HeavyAttack), RpcTarget.All, mouseController.GetInputDirection());
    }
    [PunRPC]
    public void RPC_HeavyAttack(MouseController.DirectionalInput direction)
    {
        isAttacking = true;
        animator.SetTrigger("HEAVY");
        animator.SetTrigger(direction.ToString());

        // choose collider to activate
        Collider collider;
        float staminaCost = 0f;
        switch (direction)
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
        StartCoroutine(PerformHeavyAttack(collider));
    }

    IEnumerator PerformHeavyAttack(Collider collider)
    {

        if (ShouldInterruptAction())
        {
            isAttacking = false;
            lastAttack = Time.time;
            animator.SetTrigger("HIT");
            yield break;
        }

        if(isAttackParried)
        {
            isAttackParried = false;
            isAttacking = false;
            lastAttack = Time.time;
            Debug.Log("parried");
            animator.SetTrigger("HIT"); //placeholder
            yield break;
        }

        yield return null; // yield 1 frame to ensure animation starts;

        canFeint = true;

        yield return new WaitForSeconds(0.4f); // feint 400ms before attack would land

        if(DoFeint())
        {
            isAttacking = false;
            lastAttack = Time.time;
            canFeint = false;
            feint = false;
            animator.SetTrigger("FEINT");
            yield break;
        }
        canFeint = false;

        yield return new WaitForSeconds(0.1f); // parry starts 300ms before attack lands

        canParry = true;

        yield return new WaitForSeconds(0.2f); // parry ends 100ms before attack lands lasts 200ms

        canParry = false;

        yield return new WaitForSeconds(0.1f);

        collider.enabled = true;

        yield return new WaitForSeconds(0.1f);

        collider.enabled = false;

        yield return new WaitForSeconds(0.3f);
        isAttacking = false;
        lastAttack = Time.time;
    }

    bool ShouldInterruptAction()
    {
        return tookHit && !hasHyperArmor && !isDodging;
    }

    bool DoFeint()
    {
        return feint;
    }

    void Feint(bool isFeint)
    {
        PV.RPC(nameof(RPC_Feint), RpcTarget.All, isFeint);
    }

    [PunRPC]
    public void RPC_Feint(bool isFeint)
    {
        feint = isFeint;
    }

    public void Dodge(bool _dodgeLeft)
    {
        isDodging = true;
        dodgeLeft = _dodgeLeft;
        PV.RPC(nameof(RPC_Dodge), RpcTarget.All, _dodgeLeft);
    }
    [PunRPC]
    public void RPC_Dodge(bool dodgeLeft)
    {
        isDodging = true;
        isInvincible = true;

        if(dodgeLeft)
            animator.SetTrigger("DODGELEFT");
        else
            animator.SetTrigger("DODGERIGHT");

        StartCoroutine(PerformDodge());
    }

    IEnumerator PerformDodge()
    {
        if (ShouldInterruptAction())
        {
            isDodging = false;
            lastDodgeTime = Time.time;
            animator.SetTrigger("HIT");
            yield break;
        }

        //UpdateAttackIndicator();
        yield return null; // yield 1 frame to ensure animation starts;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        yield return new WaitForSeconds(iFrameDuration);

        isInvincible = false;

        yield return new WaitForSeconds(stateInfo.length * 0.5f);

        isDodging = false;
        lastDodgeTime = Time.time;
    }
    public void LockOntoOpponent()
    {
        if(detectionRadius.opponentsInRange.Count > 0)
        {
            CheckWhoCanLock();
        }

        if(!cameraController.currentLock)
        {
            if (opponentsInFOV.Count > 0 && opponentsInFOV[0])
                cameraController.currentLock = opponentsInFOV[0].transform;
            else
                cameraController.orientationInitialFwd = orientation.forward;
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
        if (isAttacking || isDodging)
            return false;

        if (lastHitTime + timeToMove > Time.time)
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
    public bool CheckIfCanParry(PlayerController enemy, MouseController.DirectionalInput enemyDir, bool _isHeavy)
    {
        //check if player is facing enemy
        Vector3 directionToPlayer = transform.position - enemy.transform.position;
        float dotProduct = Vector3.Dot(orientation.forward, directionToPlayer.normalized);

        MouseController.DirectionalInput incomingDir = enemyDir;
        if (dotProduct < 0)
        {
            switch (enemyDir)
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
            playerIDParried = enemy.PV.ViewID;
            return true;
        }
        else
            return false;
    }
    public void ParryAttack(bool _isHeavy)
    {
        PV.RPC(nameof(RPC_ParryAttack), RpcTarget.All, _isHeavy);
        PV.RPC(nameof(RPC_GetParried), PhotonView.Find(playerIDParried).Owner); // parry reaction for the one who got parried
    }
    [PunRPC]
    public void RPC_ParryAttack(bool _isHeavy) 
    {
        lastHitTime = Time.time;
        if (_isHeavy)
        {
            // longer stun time if is heavy
            animator.SetTrigger("PARRY");
        }
        else
        {
            animator.SetTrigger("PARRY");
        }
    }

    [PunRPC]
    public void RPC_GetParried()
    {
        isAttackParried = true;
    }
    public void CheckIfBlocked(PlayerController enemy, MouseController.DirectionalInput enemyDir, int damage, bool _isHeavy)
    {
        if (isAttacking)
        {
            TakeDamage(damage);
            return;
        }


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
            BlockAttack(damage, _isHeavy);
        }
        else
            TakeDamage(damage);
    }

    public void BlockAttack(float damage, bool _isHeavy)
    {
        PV.RPC(nameof(RPC_BlockAttack), RpcTarget.All, damage, _isHeavy);
    }
    [PunRPC]
    void RPC_BlockAttack(float damage, bool _isHeavy, PhotonMessageInfo info)
    {
        lastHitTime = Time.time;
        if(_isHeavy)
        {
            // take reduced damage if isHeavy
            //currentHealth -= damage;
            //UpdateHealthBar();
        }
        animator.SetTrigger("BLOCK");
    }

    public void TakeDamage(float damage)
    {
        PV.RPC(nameof(RPC_TakeDamage), RpcTarget.All, damage);
    }

    [PunRPC]
    void RPC_TakeDamage(float damage, PhotonMessageInfo info)
    {
        tookHit = true;
        lastHitTime = Time.time;

        currentHealth -= damage;
        UpdateHealthBar();

        //Debug.Log($"Hit by {info.Sender}");

        if (currentHealth <= 0)
        {
            Die();
            animator.SetTrigger("DEATH");
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
