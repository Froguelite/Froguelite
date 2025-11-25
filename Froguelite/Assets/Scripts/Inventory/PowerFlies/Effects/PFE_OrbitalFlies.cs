using UnityEngine;

[CreateAssetMenu(fileName = "New Orbital Flies Effect", menuName = "Froguelite/Power Fly Effects/Orbital Flies")]
public class PFE_OrbitalFlies : PowerFlyEffect
{
    public override void ApplyEffect()
    {
        // Find or create OrbitalFlyManager
        OrbitalFlyManager manager = OrbitalFlyManager.Instance;

        if (manager == null)
        {
            // Attach manager to player if it doesn't exist
            if (PlayerMovement.Instance != null)
            {
                manager = PlayerMovement.Instance.gameObject.AddComponent<OrbitalFlyManager>();
            }
            else
            {
                Debug.LogError("PFE_OrbitalFlies: Cannot find player to attach OrbitalFlyManager!");
                return;
            }
        }

        // Add one orbital fly
        manager.AddOrbitalFly();
    }
}

