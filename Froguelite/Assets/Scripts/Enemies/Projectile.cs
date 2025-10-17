using System.Collections;
using UnityEngine;

public class Projectile : MonoBehaviour
{

    // Projectile is a simple "move this way for this long" script for enemy projectiles.


    #region VARIABLES


    [System.Serializable]
    public class ProjectileData
    {
        public float speed = 5f;
        public float lifetime = 3f;
        public float rotationSpeed = 2f;
        public Sprite projectileSprite;
        public Color dissipationColor;
        public float scale = 2f;
    }

    private ProjectileData projectileData;
    private Vector2 moveDirection;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private FlipbookAnimator dissipationAnimator;
    [SerializeField] private SpriteRenderer dissipationSpriteRenderer;
    [SerializeField] private Rigidbody2D rb;
    private bool destroyed = false;


    #endregion


    #region PROJECTILE


    // Initializes the projectile with its data and direction
    public void InitializeProjectile(ProjectileData data, Vector2 direction)
    {
        projectileData = data;
        moveDirection = direction.normalized;
        spriteRenderer.sprite = projectileData.projectileSprite;
        rb.linearVelocity = moveDirection * projectileData.speed;
        spriteRenderer.transform.LeanRotateAroundLocal(Vector3.forward, 360f, projectileData.rotationSpeed).setLoopClamp();
        spriteRenderer.transform.localScale = Vector3.one * projectileData.scale;
        StartCoroutine(WaitThenDestroy());
    }


    private IEnumerator WaitThenDestroy()
    {
        yield return new WaitForSeconds(projectileData.lifetime);
        DestroyProjectile();
    }


    void OnTriggerEnter2D(Collider2D collision)
    {
        if (destroyed)
            return;

        if (collision.CompareTag("Player"))
        {
            StatsManager.Instance.playerHealth.DamagePlayer(1);
            DestroyProjectile();
        }
        else if (!collision.CompareTag("Enemy"))
        {
            DestroyProjectile();
        }
    }


    private void DestroyProjectile()
    {
        if (destroyed)
            return;
            
        destroyed = true;

        rb.linearVelocity = Vector2.zero;
        spriteRenderer.enabled = false;

        dissipationSpriteRenderer.color = projectileData.dissipationColor;
        dissipationAnimator.Play(true);
        Destroy(gameObject, 1f);
    }


    #endregion


}
