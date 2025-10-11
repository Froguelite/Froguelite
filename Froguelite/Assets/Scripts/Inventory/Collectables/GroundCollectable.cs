using UnityEngine;

public abstract class GroundCollectable : MonoBehaviour, ICollectable
{

    // GroundCollectable is the base class for "standard" collectables that the player can pick up from the ground.


    #region VARIABLES


    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected Sprite displayImg;
    [SerializeField] protected float rotationAmount = 10f;
    [SerializeField] protected float rotationFrequency = 0.75f;
    [SerializeField] protected float bobbingAmount = 0.1f;
    [SerializeField] protected float bobbingFrequency = 1f;
    
    protected bool hasBeenCollected = false;


    #endregion


    #region ICOLLECTABLE


    public abstract void OnCollect();


    #endregion


    #region SETUP AND HELPERS


    // Start, setup sprite and floating animation
    protected virtual void Start()
    {
        if (spriteRenderer != null && displayImg != null)
        {
            spriteRenderer.sprite = displayImg;
        }

        // Start floating animation
        StartFloatingAnimation();
    }

    // Create floating and rotating animation using LeanTween
    protected virtual void StartFloatingAnimation()
    {
        if (spriteRenderer != null)
        {
            Transform spriteTransform = spriteRenderer.transform;
            spriteTransform.Rotate(0f, 0f, -rotationAmount / 2f);

            // Bobbing up and down animation with random start delay
            float randomBobbingDelay = Random.Range(0f, bobbingFrequency * 2f);
            LeanTween.moveLocalY(spriteTransform.gameObject, spriteTransform.localPosition.y + bobbingAmount, bobbingFrequency)
                .setEase(LeanTweenType.easeInOutSine)
                .setLoopPingPong()
                .setDelay(randomBobbingDelay);

            // Gentle rotation animation with random start delay
            float randomRotationDelay = Random.Range(0f, rotationFrequency * 2f);
            LeanTween.rotateZ(spriteTransform.gameObject, rotationAmount, rotationFrequency)
                .setEase(LeanTweenType.easeInOutSine)
                .setLoopPingPong()
                .setDelay(randomRotationDelay);
        }
    }


    #endregion


}
