using UnityEngine;
using UnityEngine.Tilemaps;

public class AmbiantParticleHandler : MonoBehaviour
{
    private Collider2D waterCollider;
    [SerializeField] private ParticleSystem waterDropParticleSystem;
    [SerializeField] private ParticleSystem rainParticleSystem;
    private ParticleSystem.Particle[] waterDropParticles;

    private bool renderWaterDrops = false;

    void Awake()
    {
        waterDropParticles = new ParticleSystem.Particle[waterDropParticleSystem.main.maxParticles];
    }

    public void ResetAmbiantParticles(bool inWorld, int zone)
    {
        // Clear all particles
        waterDropParticleSystem.Clear();
        waterDropParticleSystem.Stop();
        renderWaterDrops = false;

        rainParticleSystem.Clear();
        rainParticleSystem.Stop();

        // If we are in the world, determine and play particles based on zone
        if (inWorld)
        {
            if (zone == 0)
            {
                // Re-get the water collider
                waterCollider = FindFirstObjectByType<TilemapCollider2D>();
                
                // Play water drop particles and rain
                waterDropParticleSystem.Play();
                renderWaterDrops = true;
                rainParticleSystem.Play();
            }
        }
    }

    void LateUpdate()
    {
        if (renderWaterDrops)
            HandleRenderWaterDrops();
    }

    private void HandleRenderWaterDrops()
    {
        int numParticlesAlive = waterDropParticleSystem.GetParticles(waterDropParticles);

        for (int i = 0; i < numParticlesAlive; i++)
        {
            Vector3 particlePosition = waterDropParticles[i].position;
            
            // Check if the particle position is over the water collider
            RaycastHit2D hit = Physics2D.Raycast(particlePosition, Vector2.zero, 0f);
            if (hit.collider != waterCollider)
            {
                // Kill the particle by setting its remaining lifetime to 0
                waterDropParticles[i].remainingLifetime = 0f;
            }
        }

        // Apply the modified particle array back to the particle system
        waterDropParticleSystem.SetParticles(waterDropParticles, numParticlesAlive);
    }
}
