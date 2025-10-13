using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputFieldGrabber : MonoBehaviour
{
    #region VARIABLES
    private string inputText;

    //[SerializeField] private TextMeshProUGUI displayInputTMP;

    public event Action<String> OnInputGrabbed;
    #endregion

    #region GET AND GRAB INPUT
    public string GetInputText()
    {
        return inputText;
    }

    public void GrabFromInputField(string text)
    {
        //Get text from input field
        inputText = text;

        //Notify subscribers that input has been grabbed
        OnInputGrabbed?.Invoke(inputText);  
    }

    #endregion
}
