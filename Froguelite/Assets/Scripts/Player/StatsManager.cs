using UnityEngine;

public class StatsManager : MonoBehaviour
{

    // StatsManager holds and manages all player stats


    #region VARIABLES


    public static StatsManager Instance;

    [Header("Combat Stats")]
    public int damage;
    public int weaponRange;
    public int knockbackForce;
    public int knockbackTime;
    public int stunTime;

    [Header("Movement Stats")]
    public int speed;

    public PlayerHealth playerHealth;


    #endregion


    #region SETUP


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }


    #endregion


}
