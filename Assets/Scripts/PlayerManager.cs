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
        player = GameObject.FindGameObjectWithTag("Player");
    }
    // Update is called once per frame
    void Update()
    {
        if (player != null)
        {
            healthbar.transform.position = Vector3.Lerp(healthbar.transform.position,Camera.main.WorldToScreenPoint(player.transform.position + new Vector3(0, 1.2f, 0)), 10* Time.deltaTime);
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
