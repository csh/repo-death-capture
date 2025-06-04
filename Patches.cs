using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace RepoDeathCapture;

[HarmonyPatch(typeof(LoadingUI))]
static class LoadingPatches
{
    [HarmonyPostfix, HarmonyPatch(nameof(LoadingUI.StartLoading))]
    private static void StartLoadingPostfix(LoadingUI __instance)
    {
        SteamTimeline.ClearTimelineTooltip(0);
        SteamTimeline.SetTimelineGameMode(TimelineGameMode.LoadingScreen);
    }

    [HarmonyPostfix, HarmonyPatch(nameof(LoadingUI.StopLoading))]
    private static void StopLoadingPostfix(LoadingUI __instance)
    {
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
        SteamTimeline.AddInstantaneousTimelineEvent(
            "Death",
            $"You died!",
            "steam_death",
            1,
            0,
            TimelineEventClipPriority.Featured
        );
    }
}