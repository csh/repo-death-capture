using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace RepoDeathCapture;

[BepInPlugin("com.smrkn.repo-death-capture", "DeathCapture", "0.1.0")]
public class SteamDeathCapture : BaseUnityPlugin
{
    internal static SteamDeathCapture Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger => Instance._logger;
    private ManualLogSource _logger => base.Logger;
    internal Harmony? Harmony { get; set; }

    internal static ConfigEntry<bool> isEnabledConfigEntry;
    internal static ConfigEntry<bool> isOverlayEnabledConfigEntry;
    internal static ConfigEntry<int> overlayDelayConfigEntry;

#pragma warning disable IDE0051
    private void Awake()
    {
        Instance = this;

        // Prevent the plugin from being deleted
        this.gameObject.transform.parent = null;
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;

        Patch();

        Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");

        isEnabledConfigEntry = Config.Bind<bool>(
            "General",
            "Record Deaths",
            true,
            "Should the mod do anything?"
        );

        isEnabledConfigEntry.SettingChanged += (sender, args) =>
        {
            if (isEnabledConfigEntry.Value) return;
            SteamTimeline.ClearTimelineTooltip(0); 
            SteamTimeline.EndGamePhase();
        };

        isOverlayEnabledConfigEntry = Config.Bind<bool>(
            "Overlay",
            "Open Overlay upon Death",
            true,
            "Would you like to open the Steam Overlay upon death?"
        );

        overlayDelayConfigEntry = Config.Bind<int>(
            "Overlay",
            "Overlay Delay",
            5,
            new ConfigDescription("Delay in seconds after death before opening the Steam Overlay.", new AcceptableValueRange<int>(2, 10))
        );
    }

    internal void Patch()
    {
        Harmony ??= new Harmony(Info.Metadata.GUID);
        Harmony.PatchAll();
    }

    internal void Unpatch()
    {
        Harmony?.UnpatchSelf();
    }
}