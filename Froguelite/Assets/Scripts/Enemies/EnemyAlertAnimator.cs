using System.Collections;
using UnityEngine;

public class EnemyAlertAnimator : MonoBehaviour
{

    // EnemyAlertAnimator handles the "alerting" animations of the enemy when the player enters a room


    #region VARIABLES


    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private FaceTargetFlipper faceTargetFlipper;
    [SerializeField] private EnemyAlertExclamation alertExclamationPrefab;
    [SerializeField] private float alertMoveDistance = 0.5f;
    [SerializeField] private float alertMoveDuration = 0.4f;

    private bool alerted = false;


    #endregion


    #region ALERT


    // Triggers the alerting
    public void TriggerAlert()
    {
        if (alerted) return; // Already alerted
        alerted = true;

        // Spawn the alerting prefab
        EnemyAlertExclamation alertInstance = Instantiate(
            alertExclamationPrefab,
            transform.position + Vector3.up * 1.5f,
            Quaternion.identity
        );

        alertInstance.AnimateAlertExclamation();

        spriteRenderer.transform.LeanMoveLocalY(spriteRenderer.transform.localPosition.y + alertMoveDistance, alertMoveDuration)
            .setLoopPingPong(1)
            .setEase(LeanTweenType.easeOutSine);

        // Face the player
        faceTargetFlipper.SetFacingTarget(true);
    }


    #endregion


}
