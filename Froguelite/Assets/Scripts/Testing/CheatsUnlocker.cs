using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CheatsUnlocker : MonoBehaviour
{

    // CheatsUnlocker allows players to unlock all shop items via a secret input sequence


    #region VARIABLES


    private bool playerInRange = false;
    private List<CheatInput> inputSequence = new List<CheatInput>();
    private readonly CheatInput[] unlockAllSequence = new CheatInput[]
    {
        CheatInput.Up,
        CheatInput.Up,
        CheatInput.Down,
        CheatInput.Down,
        CheatInput.Left,
        CheatInput.Right,
        CheatInput.Left,
        CheatInput.Right,
        CheatInput.Attack
    };

    private readonly CheatInput[] goldenFliesSequence = new CheatInput[]
    {
        CheatInput.Up,
        CheatInput.Left,
        CheatInput.Down,
        CheatInput.Right,
        CheatInput.Up,
        CheatInput.Left,
        CheatInput.Down,
        CheatInput.Right,
        CheatInput.Attack
    };

    private const float inputTimeout = 2f;
    private float lastInputTime;


    #endregion


    #region ENUMS


    private enum CheatInput
    {
        Up,
        Down,
        Left,
        Right,
        Attack
    }


    #endregion


    #region MONOBEHAVIOUR


    void Start()
    {
        lastInputTime = Time.time;
    }


    void Update()
    {
        if (!playerInRange)
            return;

        // Reset sequence if too much time has passed
        if (Time.time - lastInputTime > inputTimeout && inputSequence.Count > 0)
        {
            inputSequence.Clear();
        }

        // Check for input
        CheckForInput();
    }


    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            inputSequence.Clear();
        }
    }


    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            inputSequence.Clear();
        }
    }


    #endregion


    #region INPUT TRACKING


    private void CheckForInput()
    {
        if (InputManager.Instance == null)
            return;

        CheatInput? detectedInput = null;

        // Check directional inputs
        Vector2 moveInput = Keyboard.current != null ? GetMoveInputFromKeyboard() : Vector2.zero;
        
        if (moveInput.y > 0.5f && Mathf.Abs(moveInput.x) < 0.5f)
            detectedInput = CheatInput.Up;
        else if (moveInput.y < -0.5f && Mathf.Abs(moveInput.x) < 0.5f)
            detectedInput = CheatInput.Down;
        else if (moveInput.x < -0.5f && Mathf.Abs(moveInput.y) < 0.5f)
            detectedInput = CheatInput.Left;
        else if (moveInput.x > 0.5f && Mathf.Abs(moveInput.y) < 0.5f)
            detectedInput = CheatInput.Right;

        // Check attack input
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            detectedInput = CheatInput.Attack;

        // Add to sequence if input detected
        if (detectedInput.HasValue)
        {
            AddInputToSequence(detectedInput.Value);
        }
    }


    private Vector2 GetMoveInputFromKeyboard()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
            input.y = 1f;
        else if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
            input.y = -1f;
        else if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
            input.x = -1f;
        else if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
            input.x = 1f;

        return input;
    }


    private void AddInputToSequence(CheatInput input)
    {
        inputSequence.Add(input);
        lastInputTime = Time.time;

        Debug.Log($"[CheatsUnlocker] Input added: {input}. Sequence length: {inputSequence.Count}");

        // Check if sequence matches unlock all
        if (CheckSequence(unlockAllSequence))
        {
            UnlockAllShopItems();
            inputSequence.Clear();
        }
        // Check if sequence matches golden flies
        else if (CheckSequence(goldenFliesSequence))
        {
            GiveGoldenFlies();
            inputSequence.Clear();
        }
        // Trim sequence if it gets too long
        else if (inputSequence.Count > Mathf.Max(unlockAllSequence.Length, goldenFliesSequence.Length))
        {
            inputSequence.RemoveAt(0);
        }
    }


    private bool CheckSequence(CheatInput[] requiredSequence)
    {
        if (inputSequence.Count != requiredSequence.Length)
            return false;

        for (int i = 0; i < requiredSequence.Length; i++)
        {
            if (inputSequence[i] != requiredSequence[i])
                return false;
        }

        return true;
    }


    #endregion


    #region UNLOCK


    private void UnlockAllShopItems()
    {
        Debug.Log("[CheatsUnlocker] Cheat code activated! Unlocking all shop items...");

        StumpUnlocksShop shop = FindFirstObjectByType<StumpUnlocksShop>();
        if (shop == null)
        {
            Debug.LogWarning("[CheatsUnlocker] StumpUnlocksShop not found in scene!");
            return;
        }

        // Get all power fly data and mark them as purchased
        PowerFlyData[] allFlyDatas = PowerFlyFactory.Instance.GetAllPowerFlyDatas();
        
        // Use reflection to access the private purchasedFlyIDs field
        var purchasedFlyIDsField = typeof(StumpUnlocksShop).GetField("purchasedFlyIDs", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (purchasedFlyIDsField != null)
        {
            HashSet<string> purchasedFlyIDs = (HashSet<string>)purchasedFlyIDsField.GetValue(shop);
            
            foreach (PowerFlyData flyData in allFlyDatas)
            {
                purchasedFlyIDs.Add(flyData.FlyID);
            }

            Debug.Log($"[CheatsUnlocker] Unlocked {allFlyDatas.Length} flies!");
            
            // Save the changes
            SaveManager.WriteToFile();
            
            // Play success sound if available
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(FlySlotsSound.FlySlotsCollect);
            }
        }
        else
        {
            Debug.LogError("[CheatsUnlocker] Could not access purchasedFlyIDs field!");
        }
    }


    private void GiveGoldenFlies()
    {
        Debug.Log("[CheatsUnlocker] Cheat code activated! Giving 50 golden flies...");

        if (GoldenFlyHUD.Instance == null)
        {
            Debug.LogWarning("[CheatsUnlocker] GoldenFlyHUD instance not found!");
            return;
        }

        GoldenFlyHUD.Instance.AddGoldenFlies(50);
        
        // Play success sound if available
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(CollectibleSound.GoldenCollect);
        }
    }


    #endregion


}
