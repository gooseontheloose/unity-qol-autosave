// AutoSaveProWindow.cs
// Control panel for the AutoSavePro background service - branded with the PawUI kit (paw-ui skill).
// Only the UI lives here: the timer, the save itself and the EditorPrefs keys stay in AutoSaveProBootstrap.
// Global namespace on purpose: AutoSaveProBootstrap refers to this type unqualified.
using System;
using UnityEditor;
using UnityEngine;
using AutoSavePro.PawUI;

internal sealed class AutoSaveProWindow : PawWindow
{
    private static readonly PawBrand BrandInfo = new PawBrand
    {
        Name = "Auto Save Pro",
        // Keep the tagline short: the hero watermark only appears when ~62px are left between it and the pill.
        Tagline = "Saves your scenes and assets on a timer",
        TaglineShort = "Timed scene + asset saver",
        Accent = PawAccent.Parse("mint"),
        Glyph = "save",
        PillOn = "ACTIVE",
        PillOff = "PAUSED",
        PillOnTooltip = "Auto Save Pro is on - your scenes and assets are saved on a timer",
        PillOffTooltip = "Auto Save Pro is off - nothing is saved automatically",
        TitleTooltip = "Auto Save Pro - set it and forget it, your project, always safe",
    };

    private static readonly string[] TabLabels = { "Autosave", "Settings" };
    private const int AutosaveTab = 0, SettingsTab = 1;
    private const int MinInterval = 1, MaxInterval = 30;
    private const string IntervalControl = "AutoSavePro.Interval";
    private const string GitHubUrl = "https://github.com/gooseontheloose";
    private const string VRChatUrl = "https://vrchat.com/home/user/usr_11357725-018b-40b3-9f1c-f891ee1001fd";
    private const string ChangeIntervalText = "Change interval \u203A";

    private readonly PawNavTile _githubTile = new PawNavTile();
    private readonly PawNavTile _vrchatTile = new PawNavTile();

    // Cached strings: rebuilt only when the value they show changes (nothing allocates per repaint).
    private int _builtInterval = -1;
    private int _builtSeconds = int.MinValue;
    private DateTime _builtLastSave = new DateTime(1, 1, 2);
    private int _builtTheme = -1;
    private string _onBody = string.Empty, _everyText = string.Empty, _everyChip = string.Empty;
    private string _countdownText = string.Empty, _lastSaveText = string.Empty;
    private float _countdownWidth = 60f, _pausedWidth = 80f;

    [MenuItem("Tools/Auto Save Pro")]
    public static void ShowWindow() => Open<AutoSaveProWindow>();

    protected override PawBrand Brand => BrandInfo;
    protected override string[] Tabs => TabLabels;
    protected override bool IsActive => AutoSaveProBootstrap.Enabled;

    private void OnInspectorUpdate()
    {
        // Smooth timer countdown
        if (AutoSaveProBootstrap.Enabled && !EditorApplication.isCompiling)
        {
            Repaint();
        }
    }

    protected override void DrawPage(int tab)
    {
        SyncStrings();
        if (tab == AutosaveTab) DrawAutosavePage();
        else DrawSettingsPage();
    }

    // ================================================================== Autosave tab
    private void DrawAutosavePage()
    {
        var on = AutoSaveProBootstrap.Enabled;
        if (PawGUI.StatusCard(ActiveT, on ? "Auto Save Pro is on" : "Auto Save Pro is off",
                on ? _onBody : "Nothing is saved automatically \u2014 if Unity crashes, unsaved scene and asset changes are gone. Flip the switch to start the timer.",
                "Pauses while scripts compile \u00B7 applies to every project on this machine.",
                on ? "Click to turn Auto Save Pro off" : "Click to turn Auto Save Pro on"))
        {
            AutoSaveProBootstrap.UpdateSettings(!on, AutoSaveProBootstrap.IntervalMinutes);
        }

        GUILayout.Space(PawGUI.CardGap + 4f);
        DrawNextSaveCard(on);
        GUILayout.Space(PawGUI.CardGap);
        DrawAboutCard();
    }

    private void DrawNextSaveCard(bool on)
    {
        PawGUI.BeginCard(PawTheme.CompactCardPad);
        PawGUI.CardHeader(PawGlyphs.Clock, PawTheme.Accent, "Next save", on ? _everyText : "Timer paused");
        GUILayout.Space(8f);

        // Countdown + "Change interval" link (same controls whether on or off, so IDs never shift).
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(_countdownText, PawTheme.Label(22, on ? PawTheme.Text : PawTheme.TextMuted, true), GUILayout.Width(on ? _countdownWidth : _pausedWidth),
            GUILayout.Height(30f));
        GUILayout.Space(6f);
        EditorGUILayout.BeginVertical();
        GUILayout.Space(11f);
        GUILayout.Label(on ? "until the next save" : "switch it on to start the timer", PawTheme.Label(11, PawTheme.TextSecondary), GUILayout.Height(16f),
            GUILayout.MinWidth(20f));
        EditorGUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(false));
        GUILayout.Space(11f);
        var linkW = PawGUI.LinkWidth(ChangeIntervalText);
        var linkRect = GUILayoutUtility.GetRect(linkW, 16f, GUILayout.Width(linkW), GUILayout.Height(16f));
        var changeInterval = PawGUI.Link(linkRect, ChangeIntervalText, true, "Open the Settings tab");
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(8f);
        var interval = Mathf.Max(1, AutoSaveProBootstrap.IntervalMinutes) * 60.0;
        var remaining = Math.Max(0.0, AutoSaveProBootstrap.NextSaveTime - EditorApplication.timeSinceStartup);
        var bar = PawGUI.LayoutProgress(on ? (float)(1.0 - remaining / interval) : 0f);
        PawGUI.Tooltip(bar, on ? "Time since the last save" : null);

        GUILayout.Space(14f);
        PawGUI.Divider();
        GUILayout.Space(12f);

        // Last save + the manual override (ApplyBar-style: status dot on the left, the one primary action on the right).
        EditorGUILayout.BeginHorizontal();
        var dot = GUILayoutUtility.GetRect(8f, 30f, GUILayout.Width(8f), GUILayout.Height(30f));
        GUILayout.Space(8f);
        var status = GUILayoutUtility.GetRect(30f, 30f, GUILayout.MinWidth(30f), GUILayout.ExpandWidth(true), GUILayout.Height(30f));
        var saved = AutoSaveProBootstrap.LastSaveTime != DateTime.MinValue;
        if (PawGUI.IsRepaint)
        {
            PawGUI.Fill(new Rect(dot.x, dot.center.y - 4f, 8f, 8f), saved ? PawTheme.Success : PawTheme.TextMuted, 4f);
            var style = PawTheme.Label(11, saved ? PawTheme.TextSecondary : PawTheme.TextMuted);
            GUI.Label(status, PawGUI.Ellipsize(_lastSaveText, style, status.width - 4f), style);
        }
        GUILayout.Space(8f);
        var saveNow = PawGUI.LayoutButton("Save now", PawButtonKind.Primary, true, PawGlyphs.Save, 30f,
            tooltip: "Save every open scene and all modified assets right now (restarts the timer)");
        EditorGUILayout.EndHorizontal();
        PawGUI.EndCard();

        // Actions last (after EndCard): a save can open a "Save Scene" dialog for untitled scenes.
        if (saveNow)
        {
            AutoSaveProBootstrap.PerformAutoSave();
            AutoSaveProBootstrap.ResetTimer();
            Notify("Saved scenes and assets");
            EndGUIPass();
        }
        else if (changeInterval)
        {
            SetTab(SettingsTab);
        }
    }

    private void DrawAboutCard()
    {
        PawGUI.BeginCard();
        PawGUI.CardHeader(PawGlyphs.Heart, PawTheme.TagPink, "About", "Quality of life for your workflow");
        PawGUI.CardText("Set it and forget it \u2014 your project, always safe.");
        GUILayout.Space(12f);

        PawGUI.TileRow(out var left, out var right);
        _githubTile.Glyph = PawGlyphs.Link;
        _githubTile.Tint = PawTheme.Accent;
        _githubTile.Label = "Made by";
        _githubTile.Value = "gooseontheloose";
        _githubTile.Tooltip = "Open github.com/gooseontheloose in your browser";
        _githubTile.External = true;
        var github = PawGUI.NavTile(left, _githubTile) == PawHit.Main;

        _vrchatTile.Glyph = PawGlyphs.Heart;
        _vrchatTile.Tint = PawTheme.TagPink;
        _vrchatTile.Label = "Say hi on VRChat";
        _vrchatTile.Value = "VRChat Profile";
        _vrchatTile.Tooltip = "Open the VRChat profile in your browser";
        _vrchatTile.External = true;
        var vrchat = PawGUI.NavTile(right, _vrchatTile) == PawHit.Main;
        PawGUI.EndCard();

        if (github) Application.OpenURL(GitHubUrl);
        if (vrchat) Application.OpenURL(VRChatUrl);
    }

    // ================================================================== Settings tab
    private void DrawSettingsPage()
    {
        var on = AutoSaveProBootstrap.Enabled;
        var current = AutoSaveProBootstrap.IntervalMinutes;

        PawGUI.BeginCard();
        PawGUI.CardHeader(PawGlyphs.Clock, PawTheme.Accent, "Save interval", _everyChip, PawTheme.AccentText, "How often Auto Save Pro saves");
        PawGUI.CardText("How often your open scenes and modified assets are saved. Shorter loses less work in a crash; longer means fewer save hitches while you work.");
        GUILayout.Space(14f);

        EditorGUI.BeginChangeCheck();
        var next = PawGUI.LayoutIntSlider(IntervalControl, current, MinInterval, MaxInterval, KeyFocus, "min", "1 min", "30 min", on,
            "Drag to change how often Auto Save Pro saves");
        if (EditorGUI.EndChangeCheck() && next != current) AutoSaveProBootstrap.UpdateSettings(on, next);

        if (!on)
        {
            GUILayout.Space(12f);
            PawGUI.Note(PawGlyphs.Pause, PawTheme.Warning, "Auto Save Pro is off \u2014 switch it on in the Autosave tab to change the interval.");
        }
        PawGUI.EndCard();

        GUILayout.Space(PawGUI.CardGap);
        PawGUI.BeginCard();
        PawGUI.CardHeader(PawGlyphs.Info, PawTheme.Accent2, "How it works");
        GUILayout.Space(6f);
        PawGUI.LogRow(PawGlyphs.Save, PawTheme.Success, "Saves every open scene and all modified assets together.", false, true);
        PawGUI.LogRow(PawGlyphs.Bolt, PawTheme.AccentText, "Starts with Unity and runs in the background \u2014 this window can stay closed.", false, false);
        PawGUI.LogRow(PawGlyphs.Pause, PawTheme.Warning, "Holds off while scripts compile; the timer starts over after each reload.", false, false);
        PawGUI.LogRow(PawGlyphs.Refresh, PawTheme.Accent2Text, "Every save restarts the timer, including Save now.", false, false);
        PawGUI.LogRow(PawGlyphs.Gear, PawTheme.TextMuted, "Stored per user in EditorPrefs, so the same settings apply to every project on this machine.", false, false, true);
        PawGUI.EndCard();
    }

    // ================================================================== cached strings
    private void SyncStrings()
    {
        var interval = AutoSaveProBootstrap.IntervalMinutes;
        if (interval != _builtInterval)
        {
            _builtInterval = interval;
            var every = interval == 1 ? "every minute" : "every " + interval + " minutes";
            _onBody = "Your open scenes and modified assets are saved " + every + " in the background \u2014 even while this window is closed.";
            _everyText = interval == 1 ? "Every minute" : "Every " + interval + " min";
            _everyChip = _everyText.ToUpperInvariant();
        }

        // Same countdown math as the original status line: whole minutes and seconds left, never negative.
        var secondsRemaining = Math.Max(0, AutoSaveProBootstrap.NextSaveTime - EditorApplication.timeSinceStartup);
        var whole = AutoSaveProBootstrap.Enabled ? (int)secondsRemaining : -1;
        if (whole != _builtSeconds)
        {
            _builtSeconds = whole;
            if (whole < 0)
            {
                _countdownText = "Paused";
            }
            else
            {
                var minutes = (int)(secondsRemaining / 60);
                var seconds = (int)(secondsRemaining % 60);
                _countdownText = $"{minutes:D2}:{seconds:D2}";
            }
        }

        var last = AutoSaveProBootstrap.LastSaveTime;
        if (last != _builtLastSave)
        {
            _builtLastSave = last;
            _lastSaveText = last == DateTime.MinValue ? "Not saved yet" : $"Last saved at {last:HH:mm:ss}";
        }

        if (_builtTheme != PawTheme.Version)
        {
            _builtTheme = PawTheme.Version;
            var big = PawTheme.Label(22, Color.white, true);
            // Fixed per state so the text after the digits doesn't jitter as they tick.
            _countdownWidth = Mathf.Ceil(PawGUI.Measure("00:00", big).x) + 4f;
            _pausedWidth = Mathf.Ceil(PawGUI.Measure("Paused", big).x) + 4f;
        }
    }
}
