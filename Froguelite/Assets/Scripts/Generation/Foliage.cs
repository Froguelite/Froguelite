using UnityEngine;

public class Foliage : MonoBehaviour
{

    // Foliage handles a single instance of foliage in a room


    #region VARIABLES


    [SerializeField] private Collider2D foliageCollider; // Collider for the foliage
    [SerializeField] private bool isDestructable = false; // Whether this foliage can be destroyed with the tongue
    [SerializeField] private bool isImpassable = true; // Whether this foliage blocks movement


    #endregion


    #region SETUP


    // Initializes the foliage
    public void Awake()
    {
        // Set collider state based on impassability - if impassable, collider is solid; if not, collider is a trigger
        if (foliageCollider != null)
        {
            foliageCollider.isTrigger = !isImpassable;
        }
    }


    #endregion


    #region ACCESSORS


    public bool IsDestructable()
    {
        return isDestructable;
    }


    public bool IsImpassable()
    {
        return isImpassable;
    }


    #endregion


    #region INTERACTIONS


    // Called when the foliage is impassable and hit by an attack
    public void OnImpassableHit()
    {
        // TODO (play sound effect, particles, etc.)
    }
    

    // Called when the foliage is destructable and hit by an attack
    public void OnDestructableHit()
    {
        Destroy(gameObject);
    }


    #endregion


}
