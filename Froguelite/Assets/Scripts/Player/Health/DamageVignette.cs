using UnityEngine;

public class DamageVignette : MonoBehaviour
{

    // DamageVignette manages the red vignette effect when the player takes damage.


    #region VARIABLES


    [SerializeField] private CanvasGroup vignetteCanvasGroup;

    private float fadeDuration = 0.3f;


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    private void Start()
    {
        vignetteCanvasGroup.alpha = 0f;

        // Subscribe to health change event to trigger vignette on damage
        StatsManager.Instance.playerHealth.onHealthDamaged.AddListener(HandleVignetteChange);
        StatsManager.Instance.playerHealth.onHealthHealed.AddListener(CheckClearVignette);
    }


    #endregion


    #region DAMAGE


    // Handles the vignette effect when the player takes damage
    //-------------------------------------//
    private void HandleVignetteChange()
    //-------------------------------------//
    {
        LeanTween.cancel(vignetteCanvasGroup.gameObject);

        // Flash the vignette to full opacity
        vignetteCanvasGroup.alpha = 1f;

        CheckClearVignette();
    }


    // Checks if vignette should be cleared or remain in a "danger zone"
    //-------------------------------------//
    private void CheckClearVignette()
    //-------------------------------------//
    {
        LeanTween.cancel(vignetteCanvasGroup.gameObject);

        // If health is in "danger zone", keep vignette at partial opacity
        if (StatsManager.Instance.playerHealth.currentHealth == 2)
        {
            vignetteCanvasGroup.LeanAlpha(0.2f, fadeDuration).setEaseInQuad();
        }
        else if (StatsManager.Instance.playerHealth.currentHealth == 1)
        {
            vignetteCanvasGroup.LeanAlpha(0.5f, fadeDuration).setEaseInQuad();
        }
        else
        {
            // Fade the vignette out fully if health is above danger zone
            vignetteCanvasGroup.LeanAlpha(0f, fadeDuration).setEaseInQuad();
        }
    }


    #endregion


}
