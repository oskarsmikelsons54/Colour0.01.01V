using UnityEngine;

public class PlayerMeleeAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 0.5f;      // How far the attack reaches
    [SerializeField] private int attackDamage = 25;         // Damage dealt per hit
    [SerializeField] private float attackRate = 1f;         // Attacks per second

    [Header("References")]
    [SerializeField] private Transform attackPoint;         // Empty GameObject where attack originates
    [SerializeField] private LayerMask enemyLayers;         // Which layers count as enemies

    private float nextAttackTime = 0f;

    void Update()
    {
        if (Time.time >= nextAttackTime)
        {
            if (Input.GetButtonDown("Fire1")) // Default: left mouse button or Ctrl
            {
                Attack();
                nextAttackTime = Time.time + 1f / attackRate;
            }
        }
    }

    private void Attack()
    {
        // Play attack animation here if you have an Animator
        // animator.SetTrigger("Attack");

        // Detect enemies in range
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        // Damage all enemies in range
        foreach (Collider2D enemy in hitEnemies)
        {
            // Assumes enemies have a script with TakeDamage(int amount)
            enemy.GetComponent<EnemyHealth>()?.TakeDamage(attackDamage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        // Draw attack range in editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}