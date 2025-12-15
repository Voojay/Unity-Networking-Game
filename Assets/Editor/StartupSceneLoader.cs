using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad] // Allows you to initialize an Editor class when Unity loads, and when your scripts are recompiled. // otherwise, a purely static class like this would not run unless called manually
public static class StartupSceneLoader // static since there will be one instance and this class will be purely for editor logic, will never be included in your built game
{
    // Note that the Editor folder that we created is NAME SENSITIVE -> This allows us to use UnityEditor as a namespace
    // For this script, we will want to load the Bootstrap scene without having to manually be in that scene before starting gaming session
    // This script is VERY reusable, even for singleplayer

    static StartupSceneLoader() // a static constructor -> called automatically due to [InitializeOnLoad]
    {
        // EditorApplication.playModeStateChanged is an event in the UnityEditor API. Fires every time you enter/exit play mode and enter/exit edit mode
        EditorApplication.playModeStateChanged += LoadStartupScene; // when a playmode state has changed, LoadStartupScene is called
    }

    private static void LoadStartupScene(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo(); // Pops up: “You have unsaved changes. Do you want to save them?”
        }

        if (state == PlayModeStateChange.EnteredPlayMode) // check if we in play mode
        {
            if (EditorSceneManager.GetActiveScene().buildIndex != 0) // not first scene in build settings
            {
                EditorSceneManager.LoadScene(0); 
            }
        }
    }
}
