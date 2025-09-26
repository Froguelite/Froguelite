using UnityEngine;

public class PowerFlyFactory : MonoBehaviour
{

    // PowerFlyFactory spawns Power Flies in the game world.


    #region VARIABLES





    #endregion


    #region MONOBEHAVIOUR AND SETUP





    #endregion


    #region SPAWNING


    public PowerFly SpawnPowerFly(PowerFlyData data, Vector3 position)
    {
        GameObject powerFlyPrefab = Resources.Load<GameObject>("Prefabs/PowerFly");
        GameObject powerFlyInstance = Instantiate(powerFlyPrefab, position, Quaternion.identity);
        PowerFly powerFlyComponent = powerFlyInstance.GetComponent<PowerFly>();
        powerFlyComponent.powerFlyData = data;
        return powerFlyComponent;
    }


    #endregion

}
