using Unity.VisualScripting;
using UnityEngine;

public class EnemyAlertExclamation : MonoBehaviour
{

    // EnemyAlertExclamation handles the display of the exclamation mark when an enemy is alerted


    #region VARIABLES


    [SerializeField] private float minVerticalDist = 0.5f, maxVerticalDist = 1.0f;
    [SerializeField] private float minHorizontalDist = 0.3f, maxHorizontalDist = 0.5f;
    [SerializeField] private float minRotation = 5f, maxRotation = 10f;
    [SerializeField] private float animationDuration = 1f;
    [SerializeField] private float scaleBounceAmount = 1.5f;



    #endregion


    #region ALERT EXCLAMATION


    public void AnimateAlertExclamation()
    {
        // Get the current position as starting point
        Vector3 startPosition = transform.position;
        Vector3 baseScale = transform.localScale;
        
        // Calculate random horizontal movement (-1 to 1, excluding very small values)
        float horizontalDirection = Random.Range(0f, 1f) > 0.5f ? 1f : -1f;
        float horizontalDistance = Random.Range(minHorizontalDist, maxHorizontalDist) * horizontalDirection;
        
        // Calculate upward movement
        float verticalDistance = Random.Range(minVerticalDist, maxVerticalDist);
        
        // Calculate rotation based on horizontal movement direction
        float rotationAmount = horizontalDirection * Random.Range(minRotation, maxRotation);
        
        // Target position
        Vector3 targetPosition = startPosition + new Vector3(horizontalDistance, verticalDistance, 0f);
        
        // Animate position with easeOutQuad for natural arc movement
        LeanTween.move(gameObject, targetPosition, animationDuration)
            .setEase(LeanTweenType.easeOutQuad);
        
        // Animate rotation (relative rotation)
        LeanTween.rotateAroundLocal(gameObject, Vector3.forward, rotationAmount, animationDuration)
            .setEase(LeanTweenType.easeOutCubic);
        
        // Animate scale for extra flair (slight bounce effect)
        LeanTween.scale(gameObject, baseScale * scaleBounceAmount, animationDuration * 0.3f)
            .setEase(LeanTweenType.easeOutBack)
            .setOnComplete(() => {
                LeanTween.scale(gameObject, baseScale, animationDuration * 0.7f)
                    .setEase(LeanTweenType.easeInBack);
            });
        
        // Fade out the sprite at the end
        LeanTween.alpha(gameObject, 0f, animationDuration * 0.4f)
            .setDelay(animationDuration * 0.6f)
            .setEase(LeanTweenType.easeInQuad)
            .setOnComplete(() => {
                gameObject.SetActive(false);
            });
    }


    #endregion


}
