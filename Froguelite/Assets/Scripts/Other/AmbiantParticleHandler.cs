using UnityEngine;
using UnityEngine.Tilemaps;

public class AmbiantParticleHandler : MonoBehaviour
{
    private Collider2D waterCollider;
    [SerializeField] private ParticleSystem waterDropParticleSystem;
    [SerializeField] private ParticleSystem rainParticleSystem;
    [SerializeField] private ParticleSystem firefliesParticleSystem;
    private ParticleSystem.Particle[] waterDropParticles;
    private ParticleSystem.Particle[] fireflyParticles;

    private bool renderWaterDrops = false;
    private bool renderFireflies = false;

    void Awake()
    {
        waterDropParticles = new ParticleSystem.Particle[waterDropParticleSystem.main.maxParticles];
        fireflyParticles = new ParticleSystem.Particle[firefliesParticleSystem.main.maxParticles];
    }

    public void ResetAmbiantParticles(bool inWorld, int zone)
    {
        // Clear all particles
        waterDropParticleSystem.Clear();
        waterDropParticleSystem.Stop();
        renderWaterDrops = false;

        firefliesParticleSystem.Clear();
        firefliesParticleSystem.Stop();
        renderFireflies = false;

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
            else
            {
                // Re-get the water collider
                waterCollider = FindFirstObjectByType<TilemapCollider2D>();

                // Play fireflies in other zones
                firefliesParticleSystem.Play();
                renderFireflies = true;
            }
        }
    }

    void LateUpdate()
    {
        if (renderWaterDrops)
            HandleRenderParticles(waterDropParticleSystem, waterDropParticles, waterCollider);
        if (renderFireflies)
            HandleRenderParticles(firefliesParticleSystem, fireflyParticles, waterCollider);
    }

    private void HandleRenderParticles(ParticleSystem particleSystem, ParticleSystem.Particle[] particles, Collider2D collider)
    {
        int numParticlesAlive = particleSystem.GetParticles(particles);

        for (int i = 0; i < numParticlesAlive; i++)
        {
            Vector3 particlePosition = particles[i].position;
            
            // Check if the particle position is over the water collider
            RaycastHit2D hit = Physics2D.Raycast(particlePosition, Vector2.zero, 0f);
            if (hit.collider != collider)
            {
                // Kill the particle by setting its remaining lifetime to 0
                particles[i].remainingLifetime = 0f;
            }
        }

        // Apply the modified particle array back to the particle system
        particleSystem.SetParticles(particles, numParticlesAlive);
    }
}
