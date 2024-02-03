using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class PlayerController : MonoBehaviourPunCallbacks, IDamageable/*, IPunObservable*/
{
    public enum Weapon
    {
        TRIDENT,
        SHORTSWORD
    }

    public Weapon weapon;

    //public static PlayerController instance;
    [SerializeField] PlayerManager playerManager;
    [SerializeField] GameManager gameManager;
    [SerializeField] PostGame postGame;

    public bool lockCursor = true;
    [Header("Stats")]
    [SerializeField] Transform canvasHolder;
    public Scoreboard scoreboard;
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
    [SerializeField] GameObject grayscale;
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
    [SerializeField] Collider attackRadius;
    public List<GameObject> opponentsInFOV = new();
    public List<PlayerController> opponentsInAttackRange = new();
    [SerializeField] Collider rHand, lHand;
    float lastHitTime;
    float timeToMove = 0.1f;
    [SerializeField] GameObject attackTrail;

    public bool isDodging = false;
    int dodgeDir = 1;
    public bool isInvincible = false;
    public float iFrameDuration = 0.2f;
    float lastDodgeTime;
    float dodgeCD = 1.0f;
    public float lightHitboxActivationTime = 0.5f;
    public float lightHitboxDeactivationTime = 0.6f;

    public float heavyHitboxActivationTime = 0.8f;
    public float heavyHitboxDeactivationTime = 0.9f;

    public List<float> lightStaminaCost = new() { 10f, 8f, 8f };
    public List<float> heavyStaminaCost = new() { 22f, 16f, 16f };
    public bool isAttacking = false;
    public bool isHeavy = false;
    float lastAttack;
    float regenDelay = 1f;
    float staminaRegenAmount = 20f;
    bool canRegenStamina = true;
    bool hasHyperArmor = false;
    bool tookHit = false;
    bool isCombo = false;
    public bool isStaggered = false;
    public bool canParry = false;
    bool canFeint = false;
    bool feint = false;
    public bool isParrying = false;
    public bool isParried = false;
    public int lockOnPlayerID = -1;
    public bool isBlocking = false;
    bool attackReceivedIsHeavy = false;
    bool isExhausted = false;
    Collider currentCollider;
    bool move = false;
    public int playerIDParried = -1;

    // to stop coroutines
    IEnumerator lightAttack;
    IEnumerator heavyAttack;
    IEnumerator dodging;
    IEnumerator currentStun;

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

    public Dictionary<string, int> attackDictionary = new Dictionary<string, int>
    {
        { "LIGHTLEFT", 13 },
        { "LIGHTRIGHT", 13 },
        { "LIGHTTOP", 15 },
        { "HEAVYLEFT", 23 },
        { "HEAVYRIGHT", 23 },
        { "HEAVYTOP", 30 }
    };

    public string currentAttack;
    //public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    //{
    //    if (stream.IsWriting)
    //    {
    //        dataFlags = 0;

    //        //if(transform.position != lastSyncedPosition)
    //        //{
    //        //    dataFlags |= POSITION_FLAG;
    //        //}

    //        //if(model.rotation != lastSyncedRotation)
    //        //{
    //        //    dataFlags |= ROTATION_FLAG;
    //        //}

    //        if (currentHealth != lastSyncedHealth)
    //        {
    //            dataFlags |= HEALTH_FLAG;
    //        }

    //        if (currentStamina != lastSyncedStamina)
    //        {
    //            dataFlags |= STAMINA_FLAG;
    //        }

    //        if (isParrying != lastSyncedParrying)
    //        {
    //            dataFlags |= PARRYING_FLAG;
    //        }

    //        if (isParried != lastSyncedParried)
    //        {
    //            dataFlags |= PARRIED_FLAG;
    //        }

    //        if (isBlocking != lastSyncedBlock)
    //        {
    //            dataFlags |= BLOCKING_FLAG;
    //        }

    //        if (tookHit != lastSyncedHit)
    //        {
    //            dataFlags |= TOOKHIT_FLAG;
    //        }

    //        stream.SendNext(dataFlags);

    //        //if((dataFlags & POSITION_FLAG) != 0)
    //        //{
    //        //    stream.SendNext(transform.position);
    //        //}

    //        //if ((dataFlags & ROTATION_FLAG) != 0)
    //        //{
    //        //    stream.SendNext(model.rotation);
    //        //}

    //        if ((dataFlags & HEALTH_FLAG) != 0)
    //        {
    //            stream.SendNext(currentHealth);
    //        }

    //        if ((dataFlags & STAMINA_FLAG) != 0)
    //        {
    //            stream.SendNext(currentStamina);
    //        }

    //        if ((dataFlags & PARRYING_FLAG) != 0)
    //        {
    //            stream.SendNext(isParrying);
    //        }

    //        if ((dataFlags & PARRIED_FLAG) != 0)
    //        {
    //            stream.SendNext(isParried);
    //        }

    //        if ((dataFlags & BLOCKING_FLAG) != 0)
    //        {
    //            stream.SendNext(isBlocking);
    //        }

    //        if ((dataFlags & TOOKHIT_FLAG) != 0)
    //        {
    //            stream.SendNext(tookHit);
    //        }

    //    }
    //    else
    //    {
    //        dataFlags = (byte)stream.ReceiveNext();

    //        //if ((dataFlags & POSITION_FLAG) != 0)
    //        //{
    //        //    transform.position = (Vector3)stream.ReceiveNext();
    //        //}

    //        //if ((dataFlags & ROTATION_FLAG) != 0)
    //        //{
    //        //    model.rotation = (Quaternion)stream.ReceiveNext();
    //        //}

    //        if ((dataFlags & HEALTH_FLAG) != 0)
    //        {
    //            currentHealth = (float)stream.ReceiveNext();
    //        }

    //        if ((dataFlags & STAMINA_FLAG) != 0)
    //        {
    //            currentStamina = (float)stream.ReceiveNext();
    //        }

    //        if ((dataFlags & PARRYING_FLAG) != 0)
    //        {
    //            isParrying = (bool)stream.ReceiveNext();
    //        }

    //        if ((dataFlags & PARRIED_FLAG) != 0)
    //        {
    //            isParried = (bool)stream.ReceiveNext();
    //        }

    //        if ((dataFlags & BLOCKING_FLAG) != 0)
    //        {
    //            isBlocking = (bool)stream.ReceiveNext();
    //        }

    //        if ((dataFlags & TOOKHIT_FLAG) != 0)
    //        {
    //            tookHit = (bool)stream.ReceiveNext();
    //        }

    //    }
    //}

    private void Start()
    {
        if (!PV.IsMine)
        {
            foreach (Transform child in cameraHolder.transform)
            {
                Destroy(child.gameObject);
            }
            foreach (Transform child in canvasHolder)
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
        gameManager = FindObjectOfType<GameManager>();
        postGame = FindObjectOfType<PostGame>();

        arenaCollider = GameObject.FindGameObjectWithTag("ArenaBounds").GetComponent<Collider>();
        rb.freezeRotation = true;
        readyToJump = true;
        moveSpeed = freeSpeed;
        grayscale.SetActive(false);
        attackTrail.SetActive(false);
        playerCollider.enabled = true;
        deathCollider.enabled = false;

        weapon = Weapon.TRIDENT;
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        currDir = MouseController.DirectionalInput.TOP;
        lastAttack = Time.time;
        lastDodgeTime = Time.time;
        lastHitTime = Time.time;

        SetWeapon();
    }

    public void SetWeapon()
    {
        int weaponid = 0;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            object playerWeapon;

            if (p.CustomProperties.TryGetValue(GladiatorInfo.PLAYER_WEAPON, out playerWeapon))
            {
                if (p.ActorNumber == PV.ControllerActorNr)
                {
                    weaponid = int.Parse(playerWeapon.ToString());
                }
            }
        }

        ControllerHolder controllers = GetComponent<ControllerHolder>();
        switch (weaponid)
        {
            case 0:
                weapon = Weapon.TRIDENT;
                break;
            case 1:
                weapon = Weapon.SHORTSWORD;
                break;
            default:
                break;
        }
        animator.runtimeAnimatorController = controllers.animators[weaponid];

    }
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("team") && targetPlayer == PV.Owner)
        {
            if (targetPlayer.CustomProperties.TryGetValue("team", out object team))
            {
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
            InterruptPlayer(0.5f, false);
        }

        if(currentStamina <=0)
        {
            isExhausted = true;
        }

        if(isExhausted)
        {
            if (PV.IsMine && !grayscale.activeInHierarchy)
                grayscale.SetActive(true);
            if (currentStamina >= maxStamina)
            {
                isExhausted = false;
                if(PV.IsMine)
                    grayscale.SetActive(false);
            }
        }
        //if(isBlocking)
        //{
        //    ResetTriggers("BLOCK");
        //    lastHitTime = Time.time;

        //    if (attackReceivedIsHeavy)
        //    {
        //        // take reduced damage if isHeavy
        //        currentHealth -= 3;
        //        UpdateHealthBar();
        //    }
        //    animator.SetTrigger("BLOCK");
        //    isBlocking = false;
        //    StartCoroutine(Blocking());
        //}

        // pressing esc toggles between hide/show


        // ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, whatIsGround);

        // handle drag
        if (grounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0;

        if (GameManager.Instance.gameState == GameManager.GameStates.POSTGAME)
        {
            lockCursor = false;
            Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !lockCursor;
        }

        if (!PV.IsMine)
            return;

        if (GameManager.Instance.gameState != GameManager.GameStates.POSTGAME && Input.GetKeyDown(KeyCode.Escape))
        {
            lockCursor = !lockCursor;
        }

        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockCursor;

        if (GameManager.Instance.gameState == GameManager.GameStates.POSTGAME)
            return;

        if (CheckIfParried() && !isParried)
        {
            Parried();
            isParried = true;
        }

        if (GameManager.Instance.gameState != GameManager.GameStates.COUNTDOWN)
        {
            rb.isKinematic = false;
            MyInput();
        }
        else
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }


        CheckWhoCanLock();

        UpdateUI();

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
                for (int i = 0; i < opponentsInAttackRange.Count; ++i)
                {
                    PlayerController enemyController = opponentsInAttackRange[i];
                    if (PV.ViewID == enemyController.lockOnPlayerID)
                    {
                        if (enemyController.canParry)
                        {
                            if (CheckIfCanParry(enemyController, enemyController.GetDir(), enemyController.isHeavy))
                            {
                                ParryAttack(enemyController.isHeavy);
                                hasParry = true;
                            }
                        }
                    }
                }
                if (!hasParry && !isParrying && !isBlocking)
                    HeavyAttack();
            }

            if (canFeint && Input.GetKeyDown(KeyCode.E))
            {
                Feint();
            }
            if (lastDodgeTime + dodgeCD < Time.time && AbleToMove())
            {
                if (Input.GetKey(KeyCode.A) && Input.GetKeyDown(KeyCode.Space))
                {
                    Dodge(1);
                }
                else if (Input.GetKey(KeyCode.D) && Input.GetKeyDown(KeyCode.Space))
                {
                    Dodge(2);
                }
                else if (Input.GetKey(KeyCode.W) && Input.GetKeyDown(KeyCode.Space))
                {
                    Dodge(3);
                }
                else if (Input.GetKey(KeyCode.S) && Input.GetKeyDown(KeyCode.Space))
                {
                    Dodge(4);
                }
            }
        }
        else
        {

        }
    }
    public void LightStagger()
    {
        PV.RPC(nameof(LightStaggerCall), RpcTarget.MasterClient);
    }

    [PunRPC]
    void LightStaggerCall()
    {
        PV.RPC(nameof(RPC_LightStagger), RpcTarget.All);
    }

    [PunRPC]
    void RPC_LightStagger()
    {
        animator.SetTrigger("PARRIED");
        currentStun = Stagger(0.6f);
        StartCoroutine(currentStun);
    }

    IEnumerator Stagger(float time) // for hit reactions
    {
        yield return null;

        isStaggered = true;
        //animator.SetTrigger("STUN")
        yield return new WaitForSeconds(time);

        animator.SetTrigger("ENDSTUN");
        isStaggered = false;

        currentStun = null;
    }
    private void InterruptPlayer(float stunTime, bool stagger)
    {
        animator.speed = 1f;
        isAttacking = false;
        canParry = false;
        move = false;
        lastAttack = Time.time;
        if (stagger)
            animator.SetTrigger("PARRIED");
        else
            animator.SetTrigger("HIT");

        if (lightAttack != null)
            StopCoroutine(lightAttack);
        if (heavyAttack != null)
            StopCoroutine(heavyAttack);
        if (dodging != null)
            StopCoroutine(dodging);
        if (currentCollider != null)
            currentCollider.enabled = false;

        if (currentStun != null)
            StopCoroutine(currentStun);
        attackTrail.SetActive(false);

        currentStun = Stagger(stunTime);
        StartCoroutine(currentStun);
    }
    private void FixedUpdate()
    {
        if (GameManager.Instance.gameState == GameManager.GameStates.POSTGAME)
            return;

        if (isDead)
            return;

        if (canRegenStamina && lastAttack + regenDelay < Time.time)
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
            MoveTowards(dodgeDir);
            MoveTowardsPoint(1.5f);
        }
        if(move)
        {
            float dir = 1.3f;
            if (isHeavy)
                dir = 1.5f;
            MoveTowardsPoint(dir);
        }

        if (!AbleToMove())
            return;

        MovePlayer();
    }

    private void MoveTowards(int dir)
    {
        switch (dir)
        {
            case 1:
                rb.AddForce(-orientation.right * 70f, ForceMode.Force);
                break;
            case 2:
                rb.AddForce(orientation.right * 70f, ForceMode.Force);
                break;
            case 3:
                rb.AddForce(orientation.forward * 70f, ForceMode.Force);
                break;
            case 4:
                rb.AddForce(-orientation.forward * 50f, ForceMode.Force);
                break;
            default:
                break;
        }
    }

    private void MoveTowardsPoint(float dist)
    {
        //transform.position = Vector3.MoveTowards(transform.position, cameraController.enemyController.attackRadius.ClosestPoint(transform.position), 1f * Time.fixedDeltaTime);
        Vector3 direction = transform.position - cameraController.enemyController.transform.position;
        transform.position = Vector3.MoveTowards(transform.position, cameraController.enemyController.transform.position + (direction.normalized * dist), 1f * Time.fixedDeltaTime);
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
        canParry = false;
        animator.SetTrigger("LIGHT");
        animator.SetTrigger(direction.ToString());
        currentAttack = "LIGHT" + direction.ToString();
        // choose collider to activate
        Collider collider;
        float staminaCost = 0f;
        collider = rHand;
        switch (direction)
        {
            case MouseController.DirectionalInput.TOP:
                staminaCost = lightStaminaCost[0];
                break;
            case MouseController.DirectionalInput.LEFT:
                staminaCost = lightStaminaCost[1];
                break;
            case MouseController.DirectionalInput.RIGHT:
                staminaCost = lightStaminaCost[2];
                break;
            default:
                break;
        }

        UseStaminaAttack(staminaCost);
        // Schedule hitbox activation and deactivation using animation events
        currentCollider = collider;
        if (lightAttack != null)
            StopCoroutine(lightAttack);
        lightAttack = PerformLightAttack(collider);
        StartCoroutine(lightAttack);
    }

    IEnumerator PerformLightAttack(Collider collider)
    {
        yield return null;
        canRegenStamina = false;
        canParry = false;
        canFeint = false;
        move = true;
        attackTrail.SetActive(true);

        float m = 1;
        if (isExhausted)
        {
            animator.speed = 0.5f;
            m = 2f;
        }

        yield return new WaitForSeconds(0.2f * m); // can parry 300ms before attack, light is 500ms;

        canParry = true;

        yield return new WaitForSeconds(0.2f * m); // parry window ends 100ms before attack;

        canParry = false;

        move = false;
        //yield return new WaitForSeconds(0.1f * m);

        collider.enabled = true;

        yield return new WaitForSeconds(0.3f * m);

        collider.enabled = false;
        attackTrail.SetActive(false);
        yield return new WaitForSeconds(0.1f * m);

        isAttacking = false;
        animator.speed = 1f;
        lastAttack = Time.time;
        canRegenStamina = true;
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
        canParry = false;
        animator.SetTrigger("HEAVY");
        animator.SetTrigger(direction.ToString());
        currentAttack = "HEAVY" + direction.ToString();
        // choose collider to activate
        Collider collider;
        float staminaCost = 0f;

        collider = rHand;
        switch (direction)
        {
            case MouseController.DirectionalInput.TOP:
                staminaCost = heavyStaminaCost[0];
                break;
            case MouseController.DirectionalInput.LEFT:
                staminaCost = heavyStaminaCost[1];
                break;
            case MouseController.DirectionalInput.RIGHT:
                staminaCost = heavyStaminaCost[2];
                break;
            default:
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
        isHeavy = true;
        move = true;
        attackTrail.SetActive(true);
        float m = 1;
        if (isExhausted)
        {
            animator.speed = 0.5f;
            m = 2f;
        }

        yield return new WaitForSeconds(0.4f * m); // feint 400ms before attack would land

        canFeint = false;

        yield return new WaitForSeconds(0.1f * m); // parry starts 300ms before attack lands

        canParry = true;

        yield return new WaitForSeconds(0.2f * m); // parry ends 100ms before attack lands lasts 200ms

        canParry = false;
        move = false;
        yield return new WaitForSeconds(0.1f * m);

        collider.enabled = true;

        yield return new WaitForSeconds(0.3f * m);

        collider.enabled = false;
        attackTrail.SetActive(false);

        yield return new WaitForSeconds(0.3f);

        isAttacking = false;
        lastAttack = Time.time;
        canRegenStamina = true;
        isHeavy = false;
        animator.speed = 1f;

        heavyAttack = null;
    }

    bool ShouldInterruptAction()
    {
        return tookHit && !hasHyperArmor && !isInvincible;
    }

    void Feint()
    {
        PV.RPC(nameof(RPC_FeintCall), RpcTarget.MasterClient);
    }

    [PunRPC]
    public void RPC_FeintCall()
    {
        PV.RPC(nameof(RPC_Feint), RpcTarget.All);
    }

    [PunRPC]
    public void RPC_Feint()
    {
        isAttacking = false;
        lastAttack = Time.time;
        canFeint = false;
        if (heavyAttack != null)
            StopCoroutine(heavyAttack);
        if (currentCollider != null)
            currentCollider.enabled = false;
        animator.SetTrigger("FEINT");
        canRegenStamina = true;
    }

    public void Dodge(int _dodgeDir)
    {
        dodgeDir = _dodgeDir;
        PV.RPC(nameof(RPC_DodgeCall), RpcTarget.MasterClient, _dodgeDir);
    }

    [PunRPC]
    public void RPC_DodgeCall(int _dodgeDir)
    {
        PV.RPC(nameof(RPC_Dodge), RpcTarget.All, _dodgeDir);
    }

    [PunRPC]
    public void RPC_Dodge(int _dodgeDir)
    {
        isDodging = true;
        isInvincible = false;

        switch (_dodgeDir)
        {
            case 1:
                animator.SetTrigger("DODGELEFT");
                break;
            case 2:
                animator.SetTrigger("DODGERIGHT");
                break;
            case 3:
                animator.SetTrigger("DODGEFWD");
                break;
            case 4:
                animator.SetTrigger("DODGEBWD");
                break;
            default:
                break;
        }

        dodging = PerformDodge();
        StartCoroutine(dodging);
    }

    IEnumerator PerformDodge()
    {
        //UpdateAttackIndicator();
        yield return null; // yield 1 frame to ensure animation starts;
        isInvincible = false;
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        yield return new WaitForSeconds(0.166f); // iframe starts at 166ms

        isInvincible = true;

        yield return new WaitForSeconds(0.134f); // iframe ends at 300ms

        isInvincible = false;

        yield return new WaitForSeconds(stateInfo.length * 0.5f);

        isDodging = false;
        lastDodgeTime = Time.time;

        dodging = null;
    }
    public void LockOntoOpponent()
    {
        if (detectionRadius.opponentsInRange.Count > 0)
        {
            CheckWhoCanLock();
        }

        if (!cameraController.currentLock)
        {
            if (opponentsInFOV.Count > 0 && opponentsInFOV[0])
                cameraController.currentLock = opponentsInFOV[0].transform;
            else
                cameraController.orientationInitialFwd = orientation.forward;
        }
    }
    public void CheckWhoCanLock()
    {
        for (int i = detectionRadius.opponentsInRange.Count - 1; i >= 0; --i)
        {
            if (!detectionRadius.opponentsInRange[i])
            {
                detectionRadius.opponentsInRange.Remove(detectionRadius.opponentsInRange[i]);
                continue;
            }


            if (detectionRadius.opponentsInRange[i].GetComponent<PlayerController>().isDead)
            {
                opponentsInFOV.Remove(detectionRadius.opponentsInRange[i]);
                detectionRadius.opponentsInRange.Remove(detectionRadius.opponentsInRange[i]);
                continue;
            }


            if (IsInCameraFrustum(i))
            {
                if (!opponentsInFOV.Contains(detectionRadius.opponentsInRange[i]) && !detectionRadius.opponentsInRange[i].GetComponent<PlayerController>().isDead)
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

    public bool AbleToMove()
    {
        if (isAttacking || isDodging || isBlocking || isStaggered)
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
    }

    [PunRPC]
    public void RPC_ParryAttackCall(bool _isHeavy, int _playerIDParried)
    {
        PV.RPC(nameof(RPC_ParryAttack), RpcTarget.All, _isHeavy);
        PV.RPC(nameof(RPC_AttackParried), RpcTarget.All, _playerIDParried, _isHeavy); // parry reaction for the one who got parried
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
    public void RPC_AttackParried(int playerParried, bool _isHeavy) // parry reaction for the one who got parried
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
        float stunTime;
        if (isHeavy)
            stunTime = 1.3f;
        else
            stunTime = 1.0f;

        Debug.Log(stunTime + PhotonNetwork.LocalPlayer.NickName);
        if(isExhausted)
        {
            stunTime += 0.5f;
        }
        InterruptPlayer(stunTime, true);
        isHeavy = false;
        yield return new WaitForSeconds(0.1f);

        isParried = false;
        canRegenStamina = true;
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

    public bool CheckIfBlocked(PlayerController enemy, MouseController.DirectionalInput enemyDir)
    {
        if (isAttacking || isStaggered)
        {
            return false;
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
            return true;
        }
        else
            return false;
    }

    public void BlockAttack(bool _isHeavy)
    {
        PV.RPC(nameof(RPC_BlockAttackCall), RpcTarget.MasterClient, _isHeavy);
    }

    [PunRPC]
    void RPC_BlockAttackCall(bool _isHeavy, PhotonMessageInfo info)
    {
        PV.RPC(nameof(RPC_BlockAttack), RpcTarget.All, _isHeavy);
    }

    [PunRPC]
    void RPC_BlockAttack(bool _isHeavy, PhotonMessageInfo info)
    {
        isBlocking = true;
        attackReceivedIsHeavy = _isHeavy;

        ResetTriggers("BLOCK");
        lastHitTime = Time.time;

        float stunTime;
        if (attackReceivedIsHeavy)
        {
            // take reduced damage if isHeavy
            currentHealth -= 3;
            UpdateHealthBar();
            stunTime = 0.6f;
        }
        else
        {
            isBlocking = false;
            stunTime = 0.1f;
        }
        animator.SetTrigger("BLOCK");

        StartCoroutine(Blocking(stunTime));
    }

    IEnumerator Blocking(float stunTime)
    {
        yield return null;

        yield return new WaitForSeconds(stunTime);

        isBlocking = false;
        animator.SetTrigger("BLOCKEND");
    }
    public void TakeDamage(float damage)
    {
        PV.RPC(nameof(RPC_TakeDamageCall), RpcTarget.MasterClient, damage);
    }

    [PunRPC]
    void RPC_TakeDamageCall(float damage, PhotonMessageInfo info)
    {
        PV.RPC(nameof(RPC_TakeDamage), RpcTarget.All, damage, info.Sender);
    }

    [PunRPC]
    void RPC_TakeDamage(float damage, Photon.Realtime.Player sender)
    {
        //ResetTriggers();
        tookHit = true;
        lastHitTime = Time.time;

        currentHealth -= damage;
        UpdateHealthBar();

        //Debug.Log($"Hit by {sender}");

        if (currentHealth <= 0)
        {
            if (PV.IsMine)
                grayscale.SetActive(false);
            ResetTriggers(null);
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
        //animator.Play("Death");
        if (PV.IsMine)
        {
            playerManager.DeathCount();
        }
        isDead = true;
        if (PhotonNetwork.IsMasterClient)
        {
            GameManager.Instance.CheckIfRoundEnd();
        }
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


    public void Respawn()
    {
        PV.RPC(nameof(RPC_Respawn), RpcTarget.All);
    }

    [PunRPC]
    public void RPC_Respawn()
    {
        //ResetTriggers();
        if (PV.IsMine)
            grayscale.SetActive(false);

        isDead = false;
        playerCollider.enabled = true;
        deathCollider.enabled = false;

        currentHealth = maxHealth;
        currentStamina = maxStamina;

        UpdateHealthBar();
        UpdateStaminaBar();

        canRegenStamina = true;
        currDir = MouseController.DirectionalInput.TOP;
        lastAttack = Time.time;
        lastDodgeTime = Time.time;
        lastHitTime = Time.time;
        animator.SetTrigger("REVIVE");
        Transform point = playerManager.RespawnPoint();

        transform.SetPositionAndRotation(point.position, point.rotation);
        attackTrail.SetActive(false);
        //cameraController.FreeLookCam();
        ChangeSpeed();
    }

    void ResetTriggers(string exclude) //Reset All the Animation Triggers so we don't have overlapping animations
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if(exclude != null)
                if (parameter.name == exclude || parameter.name == "CombatStance")
                    continue;
            if (parameter.name == "Yaxis" || parameter.name == "Xaxis")
                continue;
            animator.ResetTrigger(parameter.name);
        }
    }

    public void ShowWinner(int team)
    {
        PV.RPC(nameof(RPC_ShowWinner), RpcTarget.All, team);
    }

    [PunRPC]
    void RPC_ShowWinner(int team)
    {
        postGame.Show(team);
    }


}
