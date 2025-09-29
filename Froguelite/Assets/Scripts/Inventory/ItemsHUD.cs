// CollectedItemsHUD.cs
using System.Collections.Generic;
using UnityEngine;

public class CollectedItemsHUD : MonoBehaviour
{
    [SerializeField] ItemRowUI rowPrefab;   // assign ItemRow prefab
    [SerializeField] Transform rowsParent;  

    private readonly Dictionary<string, ItemRowUI> rows = new();
    private InventoryManager inv;

    void Awake()
    {
        if (!rowsParent) rowsParent = transform;
    }

    void OnEnable()
    {
        inv = InventoryManager.Instance;
        Debug.Log($"HUD OnEnable, inv = { (inv == null ? "null" : inv.name) }");

        if (inv == null) return;

        inv.OnItemChanged += HandleItemChanged;

        foreach (var kv in inv.Items)
            HandleItemChanged(kv.Value);
    }

    void OnDisable()
    {
        if (inv != null) inv.OnItemChanged -= HandleItemChanged;
        inv = null;
    }

    void HandleItemChanged(InventoryManager.Entry e)
    {
        Debug.Log($"HUD change: {e.id} x{e.count}");
        if (!rows.TryGetValue(e.id, out var row))
        {
            row = Instantiate(rowPrefab, rowsParent);
            rows[e.id] = row;
        }

        row.Set(e.display, e.icon, e.count);
        row.gameObject.SetActive(e.count > 0); // hide if count goes to 0
    }
}
