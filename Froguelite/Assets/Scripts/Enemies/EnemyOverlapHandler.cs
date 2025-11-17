using UnityEngine;

public class EnemyOverlapHandler : MonoBehaviour
{

    // EnemyOverlapHandler can be placed on any enemy collider to deal damage to the player


    #region VARIABLES


    [SerializeField] private int damage;
    [SerializeField] private EnemyBase associatedEnemy;
    [SerializeField] private bool applyKnockbackOnDamage = true;
    private IEnemy associatedEnemyScript;


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    // Awake
    private void Awake()
    {
        associatedEnemyScript = associatedEnemy.GetComponent<IEnemy>();
    }


    #endregion


    #region OVERLAP


    // OnTriggerEnter2D
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (associatedEnemy.isDead)
            return;

        if (collision.CompareTag("Player"))
        {
            // Don't damage player if they are dashing
            if (PlayerMovement.Instance.IsDashing)
                return;

            StatsManager.Instance.playerHealth.DamagePlayer(damage);
            
            if (applyKnockbackOnDamage)
                associatedEnemyScript.ApplyKnockback(StatsManager.Instance.playerKnockback.GetValue());
        }
    }


    #endregion


}
