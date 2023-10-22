using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController instance;

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

    public Animator animator;
    private void Start()
    {
        if (!instance)
            instance = this;
        else
            Destroy(this);

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        readyToJump = true;
        moveSpeed = freeSpeed;
    }

    private void Update()
    {
        // ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, whatIsGround);

        MyInput();
        SpeedControl();

        if(CameraController.instance.CombatMode)
        {
            speedPercent = new Vector2(Mathf.Clamp(orientation.InverseTransformDirection(rb.velocity).x, -1f, 1f), Mathf.Clamp(orientation.InverseTransformDirection(rb.velocity).z, -1f, 1f));
            animator.SetFloat("Xaxis", speedPercent.x, 0.1f, Time.deltaTime);
            animator.SetFloat("Yaxis", speedPercent.y, 0.1f, Time.deltaTime);

            if (Input.GetMouseButtonDown(0) && AbleToMove())
            {
                //Debug.Log("Light" + MouseController.instance.GetInputDirection().ToString());
           
                animator.SetTrigger("LIGHT");
                animator.SetTrigger(MouseController.instance.GetInputDirection().ToString());
            }
            if (Input.GetMouseButtonDown(1) && AbleToMove())
            {
                Debug.Log("Heavy" + MouseController.instance.GetInputDirection().ToString());

                //animator.SetTrigger("LIGHT");
                //animator.SetTrigger(MouseController.instance.GetInputDirection().ToString());
            }

        }
        else
        {
            speedPercent = new Vector2(Mathf.Abs(Mathf.Clamp(orientation.InverseTransformDirection(rb.velocity).x, -1f, 1f)), Mathf.Abs(Mathf.Clamp(orientation.InverseTransformDirection(rb.velocity).z, -1f, 1f)));
            //animator.SetFloat("Xaxis", speedPercent.x, 0.1f, Time.deltaTime);
            animator.SetFloat("Yaxis", Mathf.Max(speedPercent.x,speedPercent.y), 0.1f, Time.deltaTime);
        }

        // handle drag
        if (grounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0;
    }

    private void FixedUpdate()
    {
        if (!AbleToMove())
            return;
        MovePlayer();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        //// when to jump
        //if (Input.GetKey(jumpKey) && readyToJump && grounded)
        //{
        //    readyToJump = false;

        //    Jump();

        //    Invoke(nameof(ResetJump), jumpCooldown);
        //}
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

    private void Jump()
    {
        // reset y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    private void ResetJump()
    {
        readyToJump = true;
    }

    private bool AbleToMove()
    {
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Movement") && !animator.GetCurrentAnimatorStateInfo(0).IsName("CombatMovement"))
            return false;
        return true;
    }
    public void ChangeSpeed()
    {
        if (CameraController.instance.CombatMode)
        {
            moveSpeed = combatSpeed;
        }
        else
            moveSpeed = freeSpeed;
    }
}
