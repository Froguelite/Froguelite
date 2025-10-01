using System.Collections.Generic;
using UnityEngine;

public class SaveExample : MonoBehaviour
{
    private int healthLevel = 100;

    private void OnEnable()
    {
        SaveManager.SaveOnQuit += SaveHealth;
    }

    private void OnDisable()
    {
        SaveManager.SaveOnQuit -= SaveHealth;
    }

    private void SaveHealth()
    {
        SaveManager.Instance.SaveForProfile(SaveVariable.PlayerHealth, healthLevel);
        Debug.Log("Health saved: " + healthLevel);
    }



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //// Pick profile
        //SaveManager.Instance.SetActiveProfile(1);

        //SaveManager.Instance.SaveForProfile(SaveVariable.PlayerHealth, 100);
        //SaveManager.Instance.SaveForProfile(SaveVariable.PlayerPosition, new Vector3(2, 5, 7));
        //SaveManager.Instance.SaveForProfile(SaveVariable.CurrentLevel, "Level_1");
        //// Example: Save a list of strings (inventory items)
        //List<string> inventory = new List<string> { "Sword", "Potion", "Shield" };
        //SaveManager.Instance.SaveForProfile(SaveVariable.Inventory, inventory);
        //SaveManager.Instance.SaveToFile();

        //// Pick profile
        //SaveManager.Instance.SetActiveProfile(1);

        //int hp = SaveManager.Instance.LoadForProfile<int>(SaveVariable.PlayerHealth);
        //Vector3 pos = SaveManager.Instance.LoadForProfile<Vector3>(SaveVariable.PlayerPosition);
        //string lvl = SaveManager.Instance.LoadForProfile<string>(SaveVariable.CurrentLevel);
        //List<string> loadedInventory = SaveManager.Instance.LoadForProfile<List<string>>(SaveVariable.Inventory);

        //Debug.Log($"Loaded values: HP={hp}, Pos={pos}, Level={lvl}");
        //Debug.Log("Loaded Inventory: " + string.Join(", ", loadedInventory));
    }

    public void TestSaveLoad()
    {
        // Pick profile
        SaveManager.Instance.SetActiveProfile(1);

        SaveManager.Instance.SaveForProfile(SaveVariable.PlayerHealth, 100);
        SaveManager.Instance.SaveForProfile(SaveVariable.PlayerPosition, new Vector3(2, 5, 7));
        SaveManager.Instance.SaveForProfile(SaveVariable.CurrentLevel, "Level_1");
        // Example: Save a list of strings (inventory items)
        List<string> inventory = new List<string> { "Sword", "Potion", "Shield" };
        SaveManager.Instance.SaveForProfile(SaveVariable.Inventory, inventory);
        SaveManager.Instance.SaveToFile();

        // Pick profile
        SaveManager.Instance.SetActiveProfile(1);

        int hp = SaveManager.Instance.LoadForProfile<int>(SaveVariable.PlayerHealth);
        Vector3 pos = SaveManager.Instance.LoadForProfile<Vector3>(SaveVariable.PlayerPosition);
        string lvl = SaveManager.Instance.LoadForProfile<string>(SaveVariable.CurrentLevel);
        List<string> loadedInventory = SaveManager.Instance.LoadForProfile<List<string>>(SaveVariable.Inventory);

        Debug.Log($"Loaded values: HP={hp}, Pos={pos}, Level={lvl}");
        Debug.Log("Loaded Inventory: " + string.Join(", ", loadedInventory));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
