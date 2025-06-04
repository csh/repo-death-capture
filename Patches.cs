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

        if (SemiFunc.IsMainMenu() || SemiFunc.RunIsLobby() || SemiFunc.RunIsLobbyMenu())
        {
            SteamTimeline.EndGamePhase();
            SteamTimeline.SetTimelineGameMode(TimelineGameMode.Menus);
        }
        else if (SemiFunc.RunIsShop())
        {
            SteamTimeline.SetTimelineTooltip("Shopping", 0);
            SteamTimeline.SetTimelineGameMode(TimelineGameMode.Staging);
        }
        else if (SemiFunc.RunIsArena())
        {
            if (SemiFunc.IsMultiplayer())
            {
                SteamTimeline.AddInstantaneousTimelineEvent(
                    "Deathmatch",
                    "Battling to become the biggest loser!",
                    "steam_combat",
                    0,
                    3,
                    TimelineEventClipPriority.Standard
                );
                SteamTimeline.SetTimelineGameMode(TimelineGameMode.Playing);
            }
            else
            {
                SteamTimeline.ClearTimelineTooltip(0);
                SteamTimeline.EndGamePhase();
            }
        }
        else if (SemiFunc.RunIsLevel())
        {
            SteamTimeline.StartGamePhase();
            SteamTimeline.SetTimelineTooltip($"Exploring {__instance.levelNameText.text}", 0);
            SteamTimeline.SetTimelineGameMode(TimelineGameMode.Playing);
        }
    }
}

[HarmonyPatch(typeof(PlayerAvatar))]
static class PlayerControllerPatches
{
    [HarmonyPostfix, HarmonyPatch(nameof(PlayerAvatar.PlayerDeathDone))]
    private static void PlayerDeathDonePostfix(PlayerAvatar __instance)
    {
        if (!SteamDeathCapture.isEnabledConfigEntry.Value) return;

        if (SemiFunc.IsMultiplayer() == false && SemiFunc.RunIsArena())
        {
            return;
        }

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