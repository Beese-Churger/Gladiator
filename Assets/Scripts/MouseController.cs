using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class MouseController : MonoBehaviourPunCallbacks
{
    public enum DirectionalInput
    {
        TOP,
        LEFT,
        RIGHT,
    }
    DirectionalInput inputDirection;
    //public static MouseController instance;
    [SerializeField] PlayerController playerController;
    [SerializeField] CameraController cameraController;
    [SerializeField] Image cursor;
    [SerializeField] LayerMask layerMask;
    [SerializeField] GameObject piechart;
    [SerializeField] GameObject directional;

    [SerializeField] GameObject[] directions;
    //[SerializeField] Transform player;
    
    public Vector2 center;
    public Vector2 pos;

    public Vector3 worldPos;

    Vector2 prevDir;
    public int sensitivity = 10;

    PhotonView PV;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        int x = (Screen.width / 2);
        int y = (Screen.height / 2);
        
        center = new Vector2(x, y);
        pos = center;

        prevDir = pos - center;
    }

    private void Awake()
    {
        PV = GetComponent<PhotonView>();
        inputDirection = DirectionalInput.TOP;
    }

    void Update()
    {
        if (!PV.IsMine)
            return;

        if(playerController.isDead)
        {
            directional.SetActive(false);
            return;
        }

        if (!cameraController.CombatMode)
            return;

        // get mouse input
        pos.x += Input.GetAxis("Mouse X") * sensitivity;
        pos.y += Input.GetAxis("Mouse Y") * sensitivity;

        // direction from center and clamp to specified bounds
        Vector2 direction = pos - center;
        float distance = Vector2.Distance(pos, center);
        float WithinBounds = Mathf.Min(100f, distance);

        // reset cursor if out of bounds
        if ((prevDir != direction.normalized) && (distance > 100))
            pos = cursor.transform.position;

        // set cursor pos
        cursor.transform.position = center + (direction.normalized * WithinBounds);


        if (playerController.isAttacking)
        {
            directions[0].GetComponent<Image>().color = new Color(1, 0, 0, 100);
            directions[1].GetComponent<Image>().color = new Color(1, 0, 0, 100);
            directions[2].GetComponent<Image>().color = new Color(1, 0, 0, 100);
        }
        else if(playerController.isStaggered)
        {
            directions[0].GetComponent<Image>().color = new Color(0, 0, 0, 100);
            directions[1].GetComponent<Image>().color = new Color(0, 0, 0, 100);
            directions[2].GetComponent<Image>().color = new Color(0, 0, 0, 100);
        }
        else
        {
            directions[0].GetComponent<Image>().color = new Color(1, 1, 1, 100);
            directions[1].GetComponent<Image>().color = new Color(1, 1, 1, 100);
            directions[2].GetComponent<Image>().color = new Color(1, 1, 1, 100);
        }
        if (playerController.AbleToMove())
        {

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            if (angle < 0)
            {
                angle += 360;
            }

            // Determine the hovered section based on the angle
            if (angle >= 0 && angle < 120 && !directions[0].activeInHierarchy)
            {
                directions[0].SetActive(true);
                directions[1].SetActive(false);
                directions[2].SetActive(false);
                inputDirection = DirectionalInput.TOP;
                UpdateDirection();
            }
            else if (angle >= 120 && angle < 240 && !directions[1].activeInHierarchy)
            {
                directions[0].SetActive(false);
                directions[1].SetActive(true);
                directions[2].SetActive(false);
                inputDirection = DirectionalInput.LEFT;
                UpdateDirection();
            }
            else if (angle >= 240 && angle < 360 && !directions[2].activeInHierarchy)
            {
                directions[0].SetActive(false);
                directions[1].SetActive(false);
                directions[2].SetActive(true);
                inputDirection = DirectionalInput.RIGHT;
                UpdateDirection();
            }
        }
        // set previous direction;
        prevDir = direction.normalized;
    }

    public void ShowDirectionals(bool show)
    {
        directional.SetActive(show);
    }

    public GameObject GetPlayerDirectional()
    {
        return directional;
    }

    public void ResetCursor()
    {
        pos = center;
    }

    public DirectionalInput GetInputDirection()
    {
        return inputDirection;
    }

    public void UpdateDirection()
    {

        if (PV.IsMine)
            PV.RPC(nameof(RPC_UpdateIndicatorDirection), RpcTarget.All,inputDirection);
    }

    [PunRPC]
    public void RPC_UpdateIndicatorDirection(DirectionalInput input)
    {
         //Debug.Log($"Player {photonView.Owner.NickName} took {input}");
         playerController.SetDir(input);
    }
}
