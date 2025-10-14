using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image MainFill; //Red Bar
    [SerializeField] private Image OverlayFill; //White delayed bar
    [SerializeField] private Image redBar, whiteBar;
    private float fullScaleX, emptyScaleX;
    [SerializeField] private TMP_Text healthText;

    private float currentHealth;
    private float maxHealth;
    private float overlaySpeed = 0.25f; //speed of white bar slide

    void Awake()
    {
        fullScaleX = redBar.rectTransform.localScale.x;
        emptyScaleX = 0f;
    }

    public void SetMaxHealth(float max)
    {
        maxHealth = max;
        currentHealth = max;
        UpdateBarImages();

        //UpdateMainFill();
        //UpdateOverlayFill();
        //UpdateHealthText();
    }

    public void SetHealth(float newHealth)
    {
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        UpdateBarImages();

        //UpdateMainFill();
        //UpdateHealthText();
    }

    private void UpdateMainFill()
    {
        float fillAmount = currentHealth / maxHealth;
        MainFill.fillAmount = fillAmount;
    }

    private void UpdateOverlayFill()
    {
        float fillAmount = currentHealth / maxHealth;
        OverlayFill.fillAmount = fillAmount;
    }

    private void UpdateHealthText()
    {
        healthText.text = $"{(int)currentHealth} / {(int)maxHealth}";
    }

    private void Update()
    {
        /*if (OverlayFill.fillAmount > MainFill.fillAmount)
        {
            OverlayFill.fillAmount = Mathf.MoveTowards(
                OverlayFill.fillAmount,
                MainFill.fillAmount,
                overlaySpeed * Time.deltaTime
             );
        }*/
    }

    public void HideHealthBar()
    {
        gameObject.SetActive(false);
    }

    private void UpdateBarImages()
    {
        float fillAmount = currentHealth / maxHealth;
        float xScale = Mathf.Lerp(emptyScaleX, fullScaleX, fillAmount);

        // Set the scale of the red bar immediately
        Vector3 redScale = redBar.rectTransform.localScale;
        redScale.x = xScale;
        redBar.rectTransform.localScale = redScale;

        // Smoothly transition the white bar to the new scale
        whiteBar.transform.LeanScaleX(xScale, 0.2f).setEaseOutQuad();

        //UpdateHealthText();
    }
}
