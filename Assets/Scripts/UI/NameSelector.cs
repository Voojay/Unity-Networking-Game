using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NameSelector : MonoBehaviour
{
    // Note: In NameInputField in hiearchy, onvaluechanged is called every time we type something 
    // -> so HandleNameChanged() is called evertime we type 
    // When using UI, be sure to have eventsystem in hierarchy
    [SerializeField] private TMP_InputField nameField;
    [SerializeField] private Button connectButton;
    [SerializeField] private int minNameLength = 1; // name check on client side + we need it on server side too
    [SerializeField] private int maxNameLength = 12;

    public const string PlayerNameKey = "PlayerName"; // public so that clientgameManager can reference it for userData assignment
    void Start()
    {
        // If we are a headless server (dedicated server; runs with no graphics/rendering), skip this scene
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) // means that we ARE headless
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1); // Load next scene
            return; // exit
        }
        // Load their name from PlayerPrefs (a class that stores Player preferences between game sessions).
        // If there isnt any, then it wil return string.empty.
        nameField.text = PlayerPrefs.GetString(PlayerNameKey, string.Empty); 
        // we call this in start cuz we are gonna have a feature where if the player has played before, 
        // we are going to load that name from last time
        HandleNameChanged(); 
    }

    // see if the connect button will be interactable by checking our criteria
    public void HandleNameChanged()
    {
        connectButton.interactable = (nameField.text.Length >= minNameLength && nameField.text.Length <= maxNameLength); // check criteria
    }

    public void Connect() // when we press connect
    {
        // save name (set the playerprefs) and go to next scene
        PlayerPrefs.SetString(PlayerNameKey, nameField.text); // set playernamekey to the namefield.text
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1); // Load next scene (by current build index + 1)
    }
}
