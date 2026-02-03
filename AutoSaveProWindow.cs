// AutoSaveProWindow.cs
// Control panel for the AutoSavePro background service.

using UnityEditor;
using UnityEngine;
using System;

public class AutoSaveProWindow : EditorWindow
{
    // UI styling
    private GUIStyle _headerStyle;
    private GUIStyle _cardStyle;
    private GUIStyle _statusStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _linkStyle;

    [MenuItem("Tools/Auto Save Pro")]
    public static void ShowWindow()
    {
        var window = GetWindow<AutoSaveProWindow>("Auto Save Pro");
        window.minSize = new Vector2(320, 260);
    }

    private void OnInspectorUpdate()
    {
        // Smooth timer countdown
        if (AutoSaveProBootstrap.Enabled && !EditorApplication.isCompiling)
        {
            Repaint();
        }
    }

    private void OnGUI()
    {
        SetupStyles();

        GUILayout.Space(8);

        // Header
        GUILayout.BeginVertical();
        GUILayout.Label("Auto Save Pro", _headerStyle);
        GUILayout.Label("Set it and forget it—your project, always safe.", _labelStyle);
        GUILayout.EndVertical();

        GUILayout.Space(10);

        // Card container
        GUILayout.BeginVertical(_cardStyle);
        GUILayout.Space(6);

        // Enable toggle
        EditorGUI.BeginChangeCheck();
        bool newEnabled = EditorGUILayout.Toggle("Enable Auto Save", AutoSaveProBootstrap.Enabled);
        int newInterval = AutoSaveProBootstrap.IntervalMinutes;

        GUILayout.Space(6);

        // Interval slider
        EditorGUI.BeginDisabledGroup(!newEnabled);
        newInterval = EditorGUILayout.IntSlider("Interval (minutes)", AutoSaveProBootstrap.IntervalMinutes, 1, 30);
        EditorGUI.EndDisabledGroup();

        if (EditorGUI.EndChangeCheck())
        {
            AutoSaveProBootstrap.UpdateSettings(newEnabled, newInterval);
        }

        GUILayout.Space(8);

        // Status line
        string statusText;
        if (AutoSaveProBootstrap.Enabled)
        {
            double secondsRemaining = AutoSaveProBootstrap.NextSaveTime - EditorApplication.timeSinceStartup;
            secondsRemaining = Math.Max(0, secondsRemaining);
            int minutes = (int)(secondsRemaining / 60);
            int seconds = (int)(secondsRemaining % 60);
            statusText = $"Status: Enabled — Next save in {minutes:D2}:{seconds:D2}";
        }
        else
        {
            statusText = "Status: Disabled";
        }

        GUILayout.Label(statusText, _statusStyle);

        // Last save time
        string lastSaveText = AutoSaveProBootstrap.LastSaveTime == DateTime.MinValue
            ? "Last save: Not yet"
            : $"Last save: {AutoSaveProBootstrap.LastSaveTime:HH:mm:ss}";
        GUILayout.Label(lastSaveText, _labelStyle);

        GUILayout.Space(10);

        // Manual save button
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Save Now", GUILayout.Width(120), GUILayout.Height(26)))
        {
            AutoSaveProBootstrap.PerformAutoSave();
            AutoSaveProBootstrap.ResetTimer();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(6);
        GUILayout.EndVertical();

        GUILayout.FlexibleSpace();

        // Footer & Credits
        GUILayout.BeginVertical(_cardStyle);
        GUILayout.Label("Auto Save Pro • Quality of life for your workflow.", _labelStyle);
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("By ", _labelStyle, GUILayout.Width(20));
        if (GUILayout.Button("gooseontheloose", _linkStyle))
        {
            Application.OpenURL("https://github.com/gooseontheloose");
        }
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("VRChat Profile", _linkStyle))
        {
            Application.OpenURL("https://vrchat.com/home/user/usr_11357725-018b-40b3-9f1c-f891ee1001fd");
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
        GUILayout.Space(4);
    }

    private void SetupStyles()
    {
        if (_headerStyle == null)
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleLeft
            };
        }

        if (_cardStyle == null)
        {
            _cardStyle = new GUIStyle("box")
            {
                padding = new RectOffset(12, 12, 8, 8),
                margin = new RectOffset(4, 4, 4, 4)
            };
        }

        if (_statusStyle == null)
        {
            _statusStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.4f, 0.8f, 1.0f) }
            };
        }

        if (_labelStyle == null)
        {
            _labelStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true
            };
        }

        if (_linkStyle == null)
        {
            _linkStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.3f, 0.6f, 1.0f) },
                hover = { textColor = new Color(0.5f, 0.8f, 1.0f) },
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };
        }
    }
}
