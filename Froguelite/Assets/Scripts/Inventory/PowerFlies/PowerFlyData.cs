using UnityEngine;

[CreateAssetMenu(fileName = "New Power Fly", menuName = "Froguelite/Power Fly")]
public class PowerFlyData : ScriptableObject
{

    // PowerFlyData is a ScriptableObject which will hold all data for a Power Fly.


    public Sprite displayImg;
    public string powerFlyName;
    public string description;
    public PowerFlyEffect effect;

}
