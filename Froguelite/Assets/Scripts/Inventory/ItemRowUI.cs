// ItemRowUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemRowUI : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] TextMeshProUGUI label;

    // Call this whenever the item changes
    public void Set(string displayName, Sprite sprite, int count)
    {
        if (icon)  icon.sprite = sprite;
        if (label) label.text  = count <= 1 ? displayName : $"{displayName} x{count}";
    }
}
