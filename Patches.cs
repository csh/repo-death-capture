using System.Collections;
using HarmonyLib;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

namespace RepoDeathCapture;

[HarmonyPatch(typeof(LoadingUI))]
static class LoadingPatches
{
    [HarmonyPostfix, HarmonyPatch(nameof(LoadingUI.StartLoading))]
    private static void StartLoadingPostfix(LoadingUI __instance)
    {
        if (!SteamDeathCapture.isEnabledConfigEntry.Value) return;

        SteamTimeline.ClearTimelineTooltip(0);
        SteamTimeline.SetTimelineGameMode(TimelineGameMode.LoadingScreen);
    }

    [HarmonyPostfix, HarmonyPatch(nameof(LoadingUI.StopLoading))]
    private static void StopLoadingPostfix(LoadingUI __instance)
    {
        if (!SteamDeathCapture.isEnabledConfigEntry.Value) return;

        SteamTimeline.StartGamePhase();
        SteamTimeline.SetTimelineTooltip($"Exploring {__instance.levelNameText.text}", 0);
        SteamTimeline.SetTimelineGameMode(TimelineGameMode.Playing);
    }
}

[HarmonyPatch(typeof(RoundDirector))]
static class RoundDirectorPatches
{
    [HarmonyPostfix, HarmonyPatch(nameof(RoundDirector.ExtractionCompleted))]
    private static void ExtractionCompletedPostfix(RoundDirector __instance)
    {
        if (!SteamDeathCapture.isEnabledConfigEntry.Value) return;

        SteamTimeline.EndGamePhase();
        SteamTimeline.SetTimelineGameMode(TimelineGameMode.Staging);
    }
}

[HarmonyPatch(typeof(PlayerAvatar))]
static class PlayerControllerPatches
{
    [HarmonyPostfix, HarmonyPatch(nameof(PlayerAvatar.PlayerDeathDone))]
    private static void PlayerDeathDonePostfix(PlayerAvatar __instance)
    {
        if (!SteamDeathCapture.isEnabledConfigEntry.Value) return;

        var handle = SteamTimeline.AddInstantaneousTimelineEvent(
            "Death",
            $"You died!",
            "steam_death",
            1,
            0,
            TimelineEventClipPriority.Featured
        );

        SteamDeathCapture.Instance.StartCoroutine(OpenSteamOverlay(handle));
    }

    private static IEnumerator OpenSteamOverlay(TimelineEventHandle handle)
    {
        if (!SteamDeathCapture.isOverlayEnabledConfigEntry.Value) yield break;
        yield return new WaitForSecondsRealtime(SteamDeathCapture.overlayDelayConfigEntry.Value);
        SteamTimeline.OpenOverlayToTimelineEvent(handle);
    }
}