using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class PlayerController : MonoBehaviourPunCallbacks, IDamageable, IPunObservable
{
    //public enum PlayerState
    //{
    //    COMBAT,
    //    PARRIED,
    //    EXHAUSTED,
    //    HIT
    //}

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

    public bool isDead = false;
    public int team = 0;
    [Header("Movement")]
    [SerializeField] Collider arenaCollider;
    Vector3 lastValidPosition;
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
    [SerializeField] Collider deathCollider;
    [SerializeField] Detect detectionRadius;
    public List<GameObject> opponentsInFOV = new();
    public List<PlayerController> opponentsInAttackRange = new();
    [SerializeField] Collider rHand, lHand;
    float lastHitTime;
    float timeToMove = 0.1f;

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
    public bool isParrying = false;
    public bool isParried = false;
    public int lockOnPlayerID = -1;
    public bool isBlocking = false;
    bool attackReceivedIsHeavy = false;
    Collider currentCollider;

    public int playerIDParried = -1;

    // to stop coroutines
    IEnumerator lightAttack;
    IEnumerator heavyAttack;
    IEnumerator dodging;
    IEnumerator parriedStun;

    // syncing variables and shit
    //private const byte POSITION_FLAG = 1 << 0;
    //private const byte ROTATION_FLAG = 1 << 1;
    private const byte HEALTH_FLAG = 1 << 0;
    private const byte STAMINA_FLAG = 1 << 1;
    private const byte PARRYING_FLAG = 1 << 2;
    private const byte PARRIED_FLAG = 1 << 3;
    private const byte BLOCKING_FLAG = 1 << 4;
    private const byte TOOKHIT_FLAG = 1 << 5;

    Vector3 lastSyncedPosition;
    Quaternion lastSyncedRotation;
    float lastSyncedHealth;
    float lastSyncedStamina;
    bool lastSyncedParrying;
    bool lastSyncedParried;
    bool lastSyncedBlock;
    bool lastSyncedHit;

    private byte dataFlags;

    PhotonView PV;
    Player player;
    public Animator animator;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            dataFlags = 0;

            //if(transform.position != lastSyncedPosition)
            //{
            //    dataFlags |= POSITION_FLAG;
            //}

            //if(model.rotation != lastSyncedRotation)
            //{
            //    dataFlags |= ROTATION_FLAG;
            //}

            if(currentHealth != lastSyncedHealth)
            {
                dataFlags |= HEALTH_FLAG;
            }

            if (currentStamina != lastSyncedStamina)
            {
                dataFlags |= STAMINA_FLAG;
            }

            if (isParrying != lastSyncedParrying)
            {
                dataFlags |= PARRYING_FLAG;
            }

            if (isParried != lastSyncedParried)
            {
                dataFlags |= PARRIED_FLAG;
            }

            if(isBlocking != lastSyncedBlock)
            {
                dataFlags |= BLOCKING_FLAG;
            }

            if (tookHit != lastSyncedHit)
            {
                dataFlags |= TOOKHIT_FLAG;
            }

            stream.SendNext(dataFlags);

            //if((dataFlags & POSITION_FLAG) != 0)
            //{
            //    stream.SendNext(transform.position);
            //}

            //if ((dataFlags & ROTATION_FLAG) != 0)
            //{
            //    stream.SendNext(model.rotation);
            //}

            if ((dataFlags & HEALTH_FLAG) != 0)
            {
                stream.SendNext(currentHealth);
            }

            if ((dataFlags & STAMINA_FLAG) != 0)
            {
                stream.SendNext(currentStamina);
            }

            if ((dataFlags & PARRYING_FLAG) != 0)
            {
                stream.SendNext(isParrying);
            }

            if ((dataFlags & PARRIED_FLAG) != 0)
            {
                stream.SendNext(isParried);
            }

            if ((dataFlags & BLOCKING_FLAG) != 0)
            {
                stream.SendNext(isBlocking);
            }

            if ((dataFlags & TOOKHIT_FLAG) != 0)
            {
                stream.SendNext(tookHit);
            }

        }
        else
        {
            dataFlags = (byte)stream.ReceiveNext();

            //if ((dataFlags & POSITION_FLAG) != 0)
            //{
            //    transform.position = (Vector3)stream.ReceiveNext();
            //}

            //if ((dataFlags & ROTATION_FLAG) != 0)
            //{
            //    model.rotation = (Quaternion)stream.ReceiveNext();
            //}

            if ((dataFlags & HEALTH_FLAG) != 0)
            {
                currentHealth = (float)stream.ReceiveNext();
            }

            if ((dataFlags & STAMINA_FLAG) != 0)
            {
                currentStamina = (float)stream.ReceiveNext();
            }

            if ((dataFlags & PARRYING_FLAG) != 0)
            {
                isParrying = (bool)stream.ReceiveNext();
            }

            if ((dataFlags & PARRIED_FLAG) != 0)
            {
                isParried = (bool)stream.ReceiveNext();
            }

            if ((dataFlags & BLOCKING_FLAG) != 0)
            {
                isBlocking = (bool)stream.ReceiveNext();
            }

            if ((dataFlags & TOOKHIT_FLAG) != 0)
            {
                tookHit = (bool)stream.ReceiveNext();
            }

        }
    }
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
        arenaCollider = GameObject.FindGameObjectWithTag("ArenaBounds").GetComponent<Collider>();
        rb.freezeRotation = true;
        readyToJump = true;
        moveSpeed = freeSpeed;

        playerCollider.enabled = true;
        deathCollider.enabled = false;

        currentHealth = maxHealth;
        currentStamina = maxStamina;
        currDir = MouseController.DirectionalInput.TOP;
        lastAttack = Time.time;
        lastDodgeTime = Time.time;
        lastHitTime = Time.time;
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if(changedProps.ContainsKey("team") && targetPlayer == PV.Owner)
        {
            if (targetPlayer.CustomProperties.TryGetValue("team", out object team))
            {
                Debug.Log(team.ToString());
                this.team = int.Parse(team.ToString());
            }
        }
    }


    private void Update()
    {
        if (isDead)
        {
            playerCollider.enabled = false;
            deathCollider.enabled = true;
            return;
        }

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
        if (ShouldInterruptAction())
        {
            tookHit = false;
            isDodging = false;
            InterruptPlayer();
        }

        if(isBlocking)
        {
            lastHitTime = Time.time;
            if (attackReceivedIsHeavy)
            {
                // take reduced damage if isHeavy
                //currentHealth -= damage;
                //UpdateHealthBar();
            }
            animator.SetTrigger("BLOCK");
            isBlocking = false;
            //StartCoroutine(Blocking());
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

        if (CheckIfParried())
        {
            Parried();            
        }
        CheckWhoCanLock();
        UpdateUI();
        MyInput();
        SpeedControl();
        ClampPositionToArenaBounds();
        if (cameraController.CombatMode)
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
                if(!hasParry && !isParrying && !isBlocking)   
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

    private void InterruptPlayer()
    {
        isAttacking = false;
        lastAttack = Time.time;
        animator.SetTrigger("HIT");

        if (lightAttack != null)
            StopCoroutine(lightAttack);
        if (heavyAttack != null)
            StopCoroutine(heavyAttack);
        if (dodging != null)
            StopCoroutine(dodging);
        if(currentCollider != null)
            currentCollider.enabled = false;

    }
    private void FixedUpdate()
    {
        if (isDead)
            return;

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
        rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        // on ground
        //if (grounded)
        //{
        //    rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        //}
    }

    public void LightAttack()
    {
        PV.RPC(nameof(RPC_LightAttackCall), RpcTarget.MasterClient, mouseController.GetInputDirection());
    }
    [PunRPC]
    public void RPC_LightAttackCall(MouseController.DirectionalInput direction)
    {
        PV.RPC(nameof(RPC_LightAttack), RpcTarget.All, direction);
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
        currentCollider = collider;
        lightAttack = PerformLightAttack(collider);
        StartCoroutine(lightAttack);
    } 

    IEnumerator PerformLightAttack(Collider collider)
    {
        yield return null; // yield 1 frame to ensure animation starts;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        yield return new WaitForSeconds(lightHitboxActivationTime);

        collider.enabled = true;

        yield return new WaitForSeconds(lightHitboxDeactivationTime - lightHitboxActivationTime);

        collider.enabled = false;

        yield return new WaitForSeconds(0.1f);
        isAttacking = false;
        lastAttack = Time.time;

        lightAttack = null;
    }

    public void HeavyAttack()
    {
        PV.RPC(nameof(RPC_HeavyAttackCall), RpcTarget.MasterClient, mouseController.GetInputDirection());
    }
    [PunRPC]
    public void RPC_HeavyAttackCall(MouseController.DirectionalInput direction)
    {
        PV.RPC(nameof(RPC_HeavyAttack), RpcTarget.All, direction);
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
        currentCollider = collider;
        heavyAttack = PerformHeavyAttack(collider);
        StartCoroutine(heavyAttack);
    }

    IEnumerator PerformHeavyAttack(Collider collider)
    {
        yield return null; // yield 1 frame to ensure animation starts;
        canRegenStamina = false;
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
        canRegenStamina = true;
        heavyAttack = null;
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
        PV.RPC(nameof(RPC_FeintCall), RpcTarget.MasterClient, isFeint);
    }
    [PunRPC]
    public void RPC_FeintCall(bool isFeint)
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
        dodgeLeft = _dodgeLeft;
        PV.RPC(nameof(RPC_DodgeCall), RpcTarget.MasterClient, _dodgeLeft);
    }
    [PunRPC]
    public void RPC_DodgeCall(bool dodgeLeft)
    {
        PV.RPC(nameof(RPC_Dodge), RpcTarget.All, dodgeLeft);
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

        dodging = PerformDodge();
        StartCoroutine(dodging);
    }

    IEnumerator PerformDodge()
    {
        //UpdateAttackIndicator();
        yield return null; // yield 1 frame to ensure animation starts;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        yield return new WaitForSeconds(iFrameDuration);

        isInvincible = false;

        yield return new WaitForSeconds(stateInfo.length * 0.5f);

        isDodging = false;
        lastDodgeTime = Time.time;

        dodging = null;
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
        for(int i = detectionRadius.opponentsInRange.Count - 1; i >= 0; --i)
        {
            if(detectionRadius.opponentsInRange[i].GetComponent<PlayerController>().isDead)
            {
                opponentsInFOV.Remove(detectionRadius.opponentsInRange[i]);
                detectionRadius.opponentsInRange.Remove(detectionRadius.opponentsInRange[i]);

                continue;
            }
            if (!detectionRadius.opponentsInRange[i])
            {
                detectionRadius.opponentsInRange.Remove(detectionRadius.opponentsInRange[i]);
                continue;
            }

            if (IsInCameraFrustum(i))
            {
                if(!opponentsInFOV.Contains(detectionRadius.opponentsInRange[i]) && !detectionRadius.opponentsInRange[i].GetComponent<PlayerController>().isDead)
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
        {
            //check if player is facing enemy
            Vector3 directionToPlayer = transform.position - detectionRadius.opponentsInRange[index].transform.position;
            float dotProduct = Vector3.Dot(orientation.forward, directionToPlayer.normalized);

            if (dotProduct < 0)
                return true; // object within camera frustrum and infront of player
        }
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
        if (isAttacking || isDodging || isBlocking)
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
        PV.RPC(nameof(RPC_ParryAttackCall), RpcTarget.MasterClient, _isHeavy, playerIDParried);
        //PV.RPC(nameof(RPC_AttackParried), RpcTarget.MasterClient, playerIDParried);
        //PV.RPC(nameof(RPC_GetParried), PhotonView.Find(playerIDParried).Owner); // parry reaction for the one who got parried
    }
    
    [PunRPC]
    public void RPC_ParryAttackCall(bool _isHeavy, int _playerIDParried)
    {
        PV.RPC(nameof(RPC_ParryAttack), RpcTarget.All, _isHeavy);
        PV.RPC(nameof(RPC_AttackParried), RpcTarget.All, _playerIDParried);
    }

    [PunRPC]
    public void RPC_ParryAttack(bool _isHeavy) 
    {
        isParrying = true;
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
        StartCoroutine(Parrying());
    }

    [PunRPC]
    public void RPC_AttackParried(int playerParried)
    {
        playerIDParried = playerParried;
    }

    IEnumerator Parrying()
    {
        yield return null;

        playerIDParried = -1;

        yield return new WaitForSeconds(0.5f);

        isParrying = false;
    }
    public void Parried()
    {
        PV.RPC(nameof(RPC_ParriedCall), RpcTarget.MasterClient);
    }
    [PunRPC]
    void RPC_ParriedCall()
    {
        PV.RPC(nameof(RPC_Parried), RpcTarget.All);
    }
    [PunRPC]
    void RPC_Parried()
    {
        StartCoroutine(GetParried());
    }

    IEnumerator GetParried()
    {
        isParried = true;
        InterruptPlayer();
        yield return new WaitForSeconds(0.5f);

        isParried = false;
    }
    public bool CheckIfParried()
    {

        if (cameraController.currentLock)
        {
            PlayerController pc = cameraController.currentLock.GetComponent<PlayerController>();
            //Debug.Log(pc.playerIDParried + " "+ PV.ViewID);
            if (pc)
            {
                if (pc.playerIDParried != -1)
                {
                    if (pc.playerIDParried == PV.ViewID)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
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
            //if(!isParrying)
            Debug.Log("Blocked" + isAttacking);
            BlockAttack(damage, _isHeavy);
        }
        else
            TakeDamage(damage);
    }

    public void BlockAttack(float damage, bool _isHeavy)
    {
        PV.RPC(nameof(RPC_BlockAttackCall), RpcTarget.MasterClient, damage, _isHeavy);
    }
    [PunRPC]
    void RPC_BlockAttackCall(float damage, bool _isHeavy, PhotonMessageInfo info)
    {
        PV.RPC(nameof(RPC_BlockAttack), RpcTarget.All, damage, _isHeavy);
    }

    [PunRPC]
    void RPC_BlockAttack(float damage, bool _isHeavy, PhotonMessageInfo info)
    {
        isBlocking = true;
        attackReceivedIsHeavy = _isHeavy;
    }
    IEnumerator Blocking()
    {
        yield return new WaitForSeconds(0.05f);

        isBlocking = false;
    }
    public void TakeDamage(float damage)
    {
        PV.RPC(nameof(RPC_TakeDamageCall), RpcTarget.MasterClient, damage);
    }
    //public void TakeDamage(float damage)
    //{
    //    PV.RPC(nameof(RPC_TakeDamage), PV.Owner, damage);
    //}


    [PunRPC]
    void RPC_TakeDamageCall(float damage, PhotonMessageInfo info)
    {
        PV.RPC(nameof(RPC_TakeDamage), RpcTarget.All, damage, info.Sender);
    }

    [PunRPC]
    void RPC_TakeDamage(float damage, Photon.Realtime.Player sender)
    {
        tookHit = true;
        lastHitTime = Time.time;

        currentHealth -= damage;
        UpdateHealthBar();

        //Debug.Log($"Hit by {sender}");

        if (currentHealth <= 0)
        {
            Die();
            animator.SetTrigger("DEATH");
            if(PV.IsMine)
                PlayerManager.Find(sender).GetKill();
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
        if (PV.IsMine)
        {
            playerManager.DeathCount();
        }
        isDead = true;
        //playerManager.Die();
    }

    void ClampPositionToArenaBounds()
    {
        Vector3 currentPosition = transform.position;

        // Calculate the closest point on the capsule collider to the player's position
        Vector3 closestPoint = arenaCollider.ClosestPoint(currentPosition);

        if (Vector3.Distance(currentPosition, closestPoint) > 0)
        {
            // Calculate the reflection vector based on the normal of the collider at the point of contact
            Vector3 normal = (currentPosition - closestPoint).normalized;
            Vector3 reflection = Vector3.Reflect(currentPosition - closestPoint, normal).normalized;

            // Update the player's position using the reflection vector
            transform.position = closestPoint + reflection * 0.05f;
        }
    }
}
