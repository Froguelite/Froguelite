// ItemRowUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemRowUI : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] TextMeshProUGUI countLabel;  // Shows just the count number

    // Call this whenever the item changes
    public void Set(string displayName, Sprite sprite, int count)
    {
        if (icon)  icon.sprite = sprite;
        
        // Show only the count as "x2", "x4" or just the number
        if (countLabel)
        {
            if (count > 1)
                countLabel.text = $"x{count}";
            else if (count == 1)
                countLabel.text = "";  // Don't show "x1", just the icon
            else
                countLabel.text = "";  // Count is 0, show nothing
        }
    }
}
