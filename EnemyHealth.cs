using Unity.VisualScripting;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    PlayerHealth userhealth;
    public StatsManager amountdamage;

    public float maxHealth = 3;
    public int damage = 2;
    public float currentHealth;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = maxHealth;
        userhealth = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerHealth>();
        amountdamage = GameObject.FindGameObjectWithTag("Player").GetComponent<StatsManager>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Destroy(gameObject);
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            userhealth.DamagePlayer(damage);
        }
        else if(collision.gameObject.tag == "Tongue")
        {
            TakeDamage(amountdamage.playerDamage.GetValue());
        }
    }
}
