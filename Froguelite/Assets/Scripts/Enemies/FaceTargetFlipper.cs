using UnityEngine;

public class FaceTargetFlipper : MonoBehaviour
{

    // FaceTargetFlipper ensures the given SpriteRenderer faces its target by flipping it based on the target's position.


    #region VARIABLES


    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform target;
    [SerializeField] private bool startFacingFlipped = false;
    [SerializeField] private bool usePlayerAsTarget = true;


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    // Start
    void Start()
    {
        SetSpriteFlipped(startFacingFlipped);

        if (usePlayerAsTarget && PlayerMovement.Instance != null)
        {
            target = PlayerMovement.Instance.transform;
        }
    }


    // Update the sprite flip based on target position
    private void Update()
    {
        if (spriteRenderer == null || target == null) return;

        // Determine if the sprite should be flipped based on target position
        bool shouldFlip = target.position.x < transform.position.x;
        if (startFacingFlipped) shouldFlip = !shouldFlip;
        SetSpriteFlipped(shouldFlip);
    }


    #endregion


    #region SPRITE FLIP LOGIC


    // Update the sprite flip
    private void SetSpriteFlipped(bool flipped)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = flipped;
        }
    }


    #endregion

}
