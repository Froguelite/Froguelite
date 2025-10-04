using TMPro;
using UnityEngine;

public class LotusHUD : MonoBehaviour
{
    [SerializeField] TMP_Text lotusTxt;

    void OnEnable() {
        InventoryManager.Instance.OnLotusesChanged += Handle;
        Handle(InventoryManager.Instance.lotuses); 
    }
    void OnDisable() {
        InventoryManager.Instance.OnLotusesChanged -= Handle;
    }
    void Handle(int value) {
        if (lotusTxt) lotusTxt.text = $"Lotuses: {value}";
    }
}
