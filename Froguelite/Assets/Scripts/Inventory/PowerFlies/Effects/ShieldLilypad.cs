using UnityEngine;

/// <summary>
/// Individual shield lilypad that orbits the player and blocks projectiles.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ShieldLilypad : MonoBehaviour
{
    private float orbitRadius;
    private float rotationSpeed;
    private float currentAngle;

    private Transform playerTransform;

    public void Initialize(float radius, float speed, float startAngle)
    {
        this.orbitRadius = radius;
        this.rotationSpeed = speed;
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
            // Try to find player again if lost
            if (PlayerMovement.Instance != null)
                playerTransform = PlayerMovement.Instance.transform;
            else
                return;
        }

        // Update rotation angle
        currentAngle += rotationSpeed * Time.deltaTime;

        // Calculate position offset from player
        Vector3 offset = new Vector3(
            Mathf.Cos(currentAngle) * orbitRadius,
            Mathf.Sin(currentAngle) * orbitRadius,
            0f
        );

        // Set world position
        transform.position = playerTransform.position + offset;

        // Rotate the sprite itself for visual effect
        transform.Rotate(Vector3.forward, rotationSpeed * Mathf.Rad2Deg * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Block enemy projectiles
        if (collision.CompareTag("Projectile"))
        {
            Projectile projectile = collision.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.DestroyProjectile();
            }
        }
    }

    private void OnDestroy()
    {
        // Notify manager that this shield is being destroyed
        if (ShieldManager.Instance != null)
        {
            ShieldManager.Instance.RemoveShield(this);
        }
    }
}

