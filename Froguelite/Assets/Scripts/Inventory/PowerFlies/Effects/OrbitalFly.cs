using UnityEngine;

/// <summary>
/// Individual orbital fly that rotates around player and damages enemies on contact.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class OrbitalFly : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private float damageCooldown = 0.3f;

    private float orbitRadius;
    private float rotationSpeed;
    private float spriteSpinSpeed;
    private float currentAngle;
    private float lastDamageTime;

    private Transform playerTransform;

    public void Initialize(float radius, float orbitSpeed, float spinSpeed, float startAngle)
    {
        this.orbitRadius = radius;
        this.rotationSpeed = orbitSpeed;
        this.spriteSpinSpeed = spinSpeed;
        this.currentAngle = startAngle;

        // Get player reference
        if (PlayerMovement.Instance != null)
        {
            playerTransform = PlayerMovement.Instance.transform;
        }

        // Ensure trigger collider
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            // Try to find player again
            if (PlayerMovement.Instance != null)
                playerTransform = PlayerMovement.Instance.transform;
            else
                return;
        }

        // Update orbital rotation angle
        currentAngle += rotationSpeed * Time.deltaTime;

        // Calculate position offset from player
        Vector3 offset = new Vector3(
            Mathf.Cos(currentAngle) * orbitRadius,
            Mathf.Sin(currentAngle) * orbitRadius,
            0f
        );

        // Set world position
        transform.position = playerTransform.position + offset;

        // Spin the sprite itself for visual effect
        transform.Rotate(Vector3.forward, spriteSpinSpeed * Time.deltaTime);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // Continuously damage enemies while in contact (with cooldown)
        if (collision.CompareTag("Enemy") && Time.time >= lastDamageTime + damageCooldown)
        {
            IEnemy enemy = collision.GetComponent<IEnemy>();
            if (enemy == null)
                enemy = collision.GetComponentInParent<IEnemy>();

            if (enemy != null && !enemy.isDead)
            {
                float damage = StatsManager.Instance.playerDamage.GetValue() * damageMultiplier;

                // Calculate knockback direction
                Vector2 knockbackDir = (collision.transform.position - transform.position).normalized;
                float knockback = 3f;

                enemy.DamageEnemy(damage, knockback);
                lastDamageTime = Time.time;
            }
        }
    }

    private void OnDestroy()
    {
        if (OrbitalFlyManager.Instance != null)
        {
            OrbitalFlyManager.Instance.RemoveOrbital(this);
        }
    }
}

