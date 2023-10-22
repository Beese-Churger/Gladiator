using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class MouseController : MonoBehaviour
{
    public enum DirectionalInput
    {
        TOP,
        LEFT,
        RIGHT,
    }

    public static MouseController instance;
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

    // Start is called before the first frame update
    void Start()
    {
        if (!instance)
            instance = this;
        else
            Destroy(this);

        Cursor.lockState = CursorLockMode.Locked;
        int x = (Screen.width / 2);
        int y = (Screen.height / 2);
        
        center = new Vector2(x, y);
        pos = center;

        prevDir = pos - center;
    }

    // Update is called once per frame
    void Update()
    {
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
        }
        else if (angle >= 120 && angle < 240 && !directions[1].activeInHierarchy)
        {
            directions[0].SetActive(false);
            directions[1].SetActive(true);
            directions[2].SetActive(false);
        }
        else if (angle >= 240 && angle < 360 && !directions[2].activeInHierarchy)
        {
            directions[0].SetActive(false);
            directions[1].SetActive(false);
            directions[2].SetActive(true);
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
}