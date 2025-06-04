using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace RepoDeathCapture;

[BepInPlugin("com.smrkn.repo-death-capture", "DeathCapture", "0.1.0")]
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