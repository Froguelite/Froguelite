using UnityEngine;

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance;
    [Header("Combat Stats")]
    public int damage;
    public int weaponRange;
    public int knockbackForce;
    public int knockbackTime;
    public int stunTime;

    [Header("Movement Stats")]
    public int speed;

    [Header("Health Stats")]
    public int maxHealth;
    public int currentHealth;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        
    }
}
