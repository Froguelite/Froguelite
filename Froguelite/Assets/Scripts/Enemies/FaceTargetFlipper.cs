using UnityEngine;

public class FaceTargetFlipper : MonoBehaviour
{

    // FaceTargetFlipper ensures the given SpriteRenderer faces its target by flipping it based on the target's position.


    #region VARIABLES


    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform target;
    [SerializeField] private bool reverseFlip = false;
    [SerializeField] private bool usePlayerAsTarget = true;
    [SerializeField] private bool faceTargetOnStart = true;

    private bool facingTarget = true;


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    // Start
    void Start()
    {
        SetSpriteFlipped(reverseFlip);

        if (usePlayerAsTarget && PlayerMovement.Instance != null)
        {
            target = PlayerMovement.Instance.transform;
        }

        if (faceTargetOnStart)
        {
            SetFacingTarget(true);
        }
        else
        {
            SetFacingTarget(false);
        }
    }


    // Update the sprite flip based on target position
    private void Update()
    {
        if (spriteRenderer == null || target == null || !facingTarget) return;

        // Determine if the sprite should be flipped based on target position
        bool shouldFlip = target.position.x < transform.position.x;
        if (reverseFlip) shouldFlip = !shouldFlip;
        SetSpriteFlipped(shouldFlip);
    }


    public void SetReverseFlip(bool reverse)
    {
        reverseFlip = reverse;
        Update();
    }


    public void SwapReverseFlip()
    {
        reverseFlip = !reverseFlip;
        Update();
    }


    public void SetFacingTarget(bool facingTarget)
    {
        this.facingTarget = facingTarget;
        Update();
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
