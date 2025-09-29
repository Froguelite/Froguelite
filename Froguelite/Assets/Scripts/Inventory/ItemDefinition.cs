using UnityEngine;

[CreateAssetMenu(menuName = "Game/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    public string id;           
    public string displayName;    
    public Sprite icon;       
    public bool showInInventory = true;
}