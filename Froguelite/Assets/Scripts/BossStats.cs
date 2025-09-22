using UnityEngine;

[CreateAssetMenu(fileName = "BossStats", menuName = "Scriptable Objects/BossStats")]
public class BossStats : ScriptableObject
{
    //Sets up base variables for all bosses
    public string bossName;
    public int maxHealth;
    public int attackDamage; //Subject to change based on seperate attacks
    public float moveSpeed;
}
