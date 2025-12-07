using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;

[InitializeOnLoad] // this attribute gets initalized the moment the scene is loaded.
public static class StartupSceneLoader
{
    /// <summary>
    /// This Static Logic class is a class that indruduces quality of life improvments to the
    /// Unity Editor. Specifically, it ensures that the startup scene (scene at build index 0)
    /// and also prompts the user to save any modified scenes before entering play mode.
    /// </summary>
    static StartupSceneLoader()
    {
        EditorApplication.playModeStateChanged += LogPlayModeState;
    }

    private static void LogPlayModeState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            // This asks you if you want to save any modified scenes before exiting edit mode.
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }
        else if (state == PlayModeStateChange.EnteredPlayMode)
        {
            if (EditorSceneManager.GetActiveScene().buildIndex != 0)
            {
                EditorSceneManager.LoadScene(0);
            }
        }
    }
}
