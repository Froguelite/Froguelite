using UnityEngine;

[CreateAssetMenu(fileName = "New Power Fly", menuName = "Froguelite/Power Fly")]
public class PowerFlyData : ScriptableObject
{

    // PowerFlyData is a ScriptableObject which will hold all data for a Power Fly.


    public enum FlyRarity { Common, Uncommon, Rare }

    [SerializeField] private string flyID; // Unique identifier for this fly (should never change)
    public Sprite displayImg;
    public string powerFlyName;
    public string description;
    public FlyRarity flyRarity;
    public PowerFlyEffect[] effects;
    public bool onlyOneAllowed = false;
    public bool isBaseSetFly = false; // If true, this fly is automatically owned by all players

    // Public getter for the ID
    public string FlyID
    {
        get
        {
            // Auto-generate ID if empty (for backwards compatibility)
            if (string.IsNullOrEmpty(flyID))
            {
                flyID = System.Guid.NewGuid().ToString();
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
            }
            return flyID;
        }
    }

}
