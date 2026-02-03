// AutoSaveProBootstrap.cs
// Ensures Auto Save Pro runs automatically on Unity load, even if the window is never opened.

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;

[InitializeOnLoad]
public static class AutoSaveProBootstrap
{
    private const string PREF_ENABLED = "AutoSavePro_Enabled";
    private const string PREF_INTERVAL_MINUTES = "AutoSavePro_IntervalMinutes";

    public static bool Enabled;
    public static int IntervalMinutes;
    public static double NextSaveTime;
    public static DateTime LastSaveTime = DateTime.MinValue;

    static AutoSaveProBootstrap()
    {
        // Load settings - Default to true for enabled
        Enabled = EditorPrefs.GetBool(PREF_ENABLED, true);
        IntervalMinutes = Mathf.Clamp(EditorPrefs.GetInt(PREF_INTERVAL_MINUTES, 5), 1, 30);

        ResetTimer();

        // Register update loop
        EditorApplication.update -= Update;
        EditorApplication.update += Update;
    }

    public static void ResetTimer()
    {
        if (Enabled)
            NextSaveTime = EditorApplication.timeSinceStartup + IntervalMinutes * 60.0;
    }

    public static void UpdateSettings(bool enabled, int interval)
    {
        bool changed = (Enabled != enabled || IntervalMinutes != interval);
        Enabled = enabled;
        IntervalMinutes = interval;
        
        EditorPrefs.SetBool(PREF_ENABLED, Enabled);
        EditorPrefs.SetInt(PREF_INTERVAL_MINUTES, IntervalMinutes);
        
        if (changed)
            ResetTimer();
    }

    private static void Update()
    {
        if (!Enabled)
            return;

        if (EditorApplication.isCompiling)
            return;

        double now = EditorApplication.timeSinceStartup;
        if (now >= NextSaveTime)
        {
            PerformAutoSave();
            NextSaveTime = now + IntervalMinutes * 60.0;
        }
    }

    public static void PerformAutoSave()
    {
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        LastSaveTime = DateTime.Now;
        Debug.Log($"[Auto Save Pro] Auto-saved at {LastSaveTime:HH:mm:ss}");
        
        // Ensure we repaint any open windows
        AutoSaveProWindow window = EditorWindow.GetWindow<AutoSaveProWindow>(false, "Auto Save Pro", false);
        if (window != null) window.Repaint();
    }
}
