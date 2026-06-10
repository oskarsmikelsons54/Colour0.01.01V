using UnityEngine;
using TMPro;

public class healthbar : MonoBehaviour
{
    [Tooltip("TextMeshPro TMP_Text component used to display the health value")]
    [SerializeField] private TMP_Text healthText;

    [Tooltip("GameObject that has the health component (EnemyHealth or PlayerHealth)")]
    [SerializeField] private GameObject targetObject;

    // Cached references to supported health components
    private EnemyHealth enemyHealth;
    private PlayerHealth playerHealth;

    // whether victory message has been shown
    private bool victoryShown = false;

    void Start()
    {
        if (healthText == null)
        {
            Debug.LogWarning("healthbar: healthText is not assigned. Please assign a TMP_Text in the inspector.");
        }

        if (targetObject == null)
        {
            Debug.LogWarning("healthbar: targetObject is not assigned. Please assign a GameObject that contains a health component.");
            return;
        }

        enemyHealth = targetObject.GetComponent<EnemyHealth>();
        playerHealth = targetObject.GetComponent<PlayerHealth>();

        if (enemyHealth == null && playerHealth == null)
        {
            Debug.LogWarning("healthbar: targetObject does not have EnemyHealth or PlayerHealth component.");
        }

        // subscribe to death event if enemy
        if (enemyHealth != null)
        {
            enemyHealth.OnDeath += OnEnemyDeath;
        }

        // Initialize text immediately
        UpdateHealthText();
    }

    void OnDestroy()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnDeath -= OnEnemyDeath;
        }
    }

    void Update()
    {
        UpdateHealthText();
    }

    private void OnEnemyDeath()
    {
        if (healthText == null) return;
        healthText.text = "YOU WIN CONGRATULATIONS WINNER!";
        victoryShown = true;
    }

    private void UpdateHealthText()
    {
        if (healthText == null || victoryShown) return;

        if (enemyHealth != null)
        {
            healthText.text = enemyHealth.currentHealth.ToString();
        }
        else if (playerHealth != null)
        {
            // PlayerHealth uses a float 'health' value; display as integer when appropriate
            healthText.text = Mathf.CeilToInt(playerHealth.health).ToString();
        }
        else
        {
            healthText.text = "";
        }
    }
}
