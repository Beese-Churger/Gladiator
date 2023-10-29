using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class PlayerManager : MonoBehaviour
{
    public static PlayerManager instance;

    public GameObject healthbar;
    public Slider healthBarFill;
    public Slider staminaBarFill;
    [SerializeField] private GameObject player;
    // Start is called before the first frame update

    public float maxHealth = 100f;
    private float currentHealth;
    void Start()
    {
        currentHealth = maxHealth;
    }

    private void Awake()
    {
        player = gameObject;
    }
    // Update is called once per frame
    void Update()
    {
        if (player != null)
        {

            healthbar.transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
            
        }

        if(Input.GetKeyDown(KeyCode.N))
        {
            SetHealth(currentHealth - 10);
            Debug.Log("hi");
        }
    }
    public void SetHealth(float health)
    {
        currentHealth = health;
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        float fillAmount = currentHealth / maxHealth;
        healthBarFill.value = fillAmount;
    }
}
