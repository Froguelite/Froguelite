using UnityEngine;

public class AttackOverlapHandler : MonoBehaviour
{

    // AttackOverlapHandler handles when the player's attack collider overlaps with other colliders


    #region VARIABLES





    #endregion


    #region OVERLAP


    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the collider belongs to an enemy
        if (collision.CompareTag("Enemy"))
        {
            Debug.Log("Enemy hit by attack!");
            // TODO
        }
        else if (collision.CompareTag("Door"))
        {
            Door door = collision.GetComponent<Door>();
            if (door != null)
            {
                door.OnInteract();
            }
        }
    }


    #endregion
    

}
