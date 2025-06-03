using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SteamDeathCapture;

[BepInPlugin("smrkn.SteamDeathCapture", "SteamDeathCapture", "1.0")]
public class SteamDeathCapture : BaseUnityPlugin
{
    internal static SteamDeathCapture Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger => Instance._logger;
    private ManualLogSource _logger => base.Logger;
    internal Harmony? Harmony { get; set; }

#pragma warning disable IDE0051
    private void Awake()
    {
        Instance = this;

        // Prevent the plugin from being deleted
        this.gameObject.transform.parent = null;
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;

        Patch();

        Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Contains("Lobby"))
        {
            SteamTimeline.EndGamePhase();
            SteamTimeline.ClearTimelineTooltip(0);
            SteamTimeline.SetTimelineGameMode(TimelineGameMode.Staging);
        }
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

    private void Update()
    {
        // Code that runs every frame goes here
    }
}