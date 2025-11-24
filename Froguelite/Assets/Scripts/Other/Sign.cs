using UnityEngine;

public class Sign : MonoBehaviour
{
    [SerializeField] private SpriteRenderer signSpriteRenderer;
    [SerializeField] private SpriteRenderer symbolSpriteRenderer;
    [SerializeField] private Sprite landSprite;
    [SerializeField] private Sprite waterSprite;
    [SerializeField] private Sprite waterForestSprite;
    [SerializeField] private ParticleSystem waterParticles;

    public void SetupSign(Sprite symbolSprite, bool inWater)
    {
        symbolSpriteRenderer.sprite = symbolSprite;

        // Position the sign based on whether it's in water or on land
        if (inWater)
        {
            if (LevelManager.Instance.currentZone == 0)
            {
                signSpriteRenderer.sprite = waterSprite;
                waterParticles.Play();
            }
            else
            {
                signSpriteRenderer.sprite = waterForestSprite;
            }
        }
        else
        {
            signSpriteRenderer.sprite = landSprite;
        }
    }
}
