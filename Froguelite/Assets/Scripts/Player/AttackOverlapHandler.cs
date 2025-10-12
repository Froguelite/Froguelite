using UnityEngine;

public class AttackOverlapHandler : MonoBehaviour
{

    // AttackOverlapHandler handles when the player's attack collider overlaps with other colliders


    #region VARIABLES


    


    #endregion


    #region OVERLAP


    private void OnTriggerEnter2D(Collider2D collision)
    {
        // If the player is not currently attacking, ignore (tongue is retracted)
        if (!PlayerAttack.Instance.IsAttacking())
            return;

        // Check if the collider belongs to an enemy
        if (collision.CompareTag("Enemy"))
        {
            IEnemy enemy = collision.GetComponent<IEnemy>();
            if (enemy == null)
                enemy = collision.GetComponentInParent<IEnemy>();

            if (enemy != null)
            {
                enemy.DamageEnemy(StatsManager.Instance.playerDamage.GetValue(), StatsManager.Instance.playerKnockback.GetValue());
                PlayerAttack.Instance.StopTongueExtension(false);
            }
        }
        // Doors
        else if (collision.CompareTag("Door"))
        {
            Door door = collision.GetComponent<Door>();
            if (door != null)
            {
                door.OnInteract();
            }
        }
        // Collectables
        else if (collision.CompareTag("Collectable"))
        {
            ICollectable collectable = collision.GetComponent<ICollectable>();
            if (collectable != null)
            {
                collectable.OnCollect();
            }
        }
        // Foliage
        else if (collision.CompareTag("Foliage"))
        {
            Foliage foliage = collision.GetComponent<Foliage>();

            if (foliage == null)
                foliage = collision.GetComponentInParent<Foliage>();
            if (foliage == null)
                return;
                
            // If it's impassable, stop the tongue extension
            if (foliage.IsImpassable())
            {
                PlayerAttack.Instance.StopTongueExtension();
                foliage.OnImpassableHit();
            }
            // If it's destructable, destroy it
            if (foliage.IsDestructable())
            {
                foliage.OnDestructableHit();
            }
        }
    }


    #endregion


}
