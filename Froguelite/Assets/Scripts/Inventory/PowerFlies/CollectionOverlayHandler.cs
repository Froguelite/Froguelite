using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class CollectionOverlayHandler : MonoBehaviour
{

    // CollectionOverlayHandler manages the overlay UI that appears when collecting power flies


    #region VARIABLES


    public static CollectionOverlayHandler Instance;

    [SerializeField] private CanvasGroup overlayCanvasGroup;
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float displayDuration = 2.5f;
    [SerializeField] private Transform contentParent;
    private float contentStartY;
    [SerializeField] private float contentMoveDistance = 50f;
    [SerializeField] private TMP_Text flyNameText;
    [SerializeField] private TMP_Text flyDescriptionText;


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        contentStartY = contentParent.localPosition.y;
    }


    #endregion


    #region TRIGGER OVERLAY


    // Triggers the overlay to show that a power fly has been collected
    public void ShowPowerFlyCollected(PowerFlyData powerFlyData)
    {
        LeanTween.cancel(overlayCanvasGroup.gameObject);
        LeanTween.cancel(contentParent.gameObject);

        overlayCanvasGroup.alpha = 0f;
        overlayCanvasGroup.LeanAlpha(1f, fadeDuration).setOnComplete(() =>
        {
            overlayCanvasGroup.LeanAlpha(0f, fadeDuration).setDelay(displayDuration);
        });

        contentParent.localPosition = new Vector3(contentParent.localPosition.x, contentStartY - contentMoveDistance, contentParent.localPosition.z);
        contentParent.LeanMoveLocalY(contentStartY, fadeDuration).setEaseOutQuad().setOnComplete(() =>
        {
            contentParent.LeanMoveLocalY(contentStartY + contentMoveDistance, fadeDuration).setDelay(displayDuration).setEaseInQuad();
        });

        flyNameText.text = powerFlyData.powerFlyName;
        flyDescriptionText.text = powerFlyData.description;

    }


    #endregion
    

}
