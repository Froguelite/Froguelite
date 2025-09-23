using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image MainFill; //Red Bar
    [SerializeField] private Image OverlayFill; //White delayed bar
    [SerializeField] private TMP_Text healthText;

    private float currentHealth;
    private float maxHealth;
    private float overlaySpeed = 0.25f; //speed of white bar slide

    public void SetMaxHealth(float max)
    {
        maxHealth = max;
        currentHealth = max;

        UpdateMainFill();
        UpdateOverlayFill();
        UpdateHealthText();
    }

    public void SetHealth(float newHealth)
    {
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);

        UpdateMainFill();
        UpdateHealthText();
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
        if (OverlayFill.fillAmount > MainFill.fillAmount)
        {
            OverlayFill.fillAmount = Mathf.MoveTowards(
                OverlayFill.fillAmount,
                MainFill.fillAmount,
                overlaySpeed * Time.deltaTime
             );
        }
    }

    public void HideHealthBar()
    {
        gameObject.SetActive(false);
    }
}
