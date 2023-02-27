﻿using System.Collections.Generic;
using System.Linq;
using EnoMod.Modules;
using HarmonyLib;
using UnityEngine;

namespace EnoMod.Patches;

[HarmonyPatch(typeof(ReactorSystemType), nameof(ReactorSystemType.RepairDamage))]
public static class ReactorSystemTypePatch
{
    public static bool Prefix(ReactorSystemType __instance, PlayerControl player, byte opCode)
    {
        if (ShipStatus.Instance.Type != ShipStatus.MapType.Pb || opCode != (byte) 128 ||
            __instance.IsActive) return true;
        __instance.Countdown = CustomSettings.ReactorCountdown;
        __instance.UserConsolePairs.Clear();
        __instance.IsDirty = true;
        return false;
    }
}

public static class BlockUtilitiesPatch
{
    public static bool IsCommsActive()
    {
        int mapId = EnoModPlugin.NormalOptions.MapId;
        if (mapId == 1)
        {
            var hqHudSystemType = ShipStatus.Instance.Systems[SystemTypes.Comms].Cast<HqHudSystemType>();
            return hqHudSystemType is { IsActive: true };
        }

        var hudOverrideSystemType = ShipStatus.Instance.Systems[SystemTypes.Comms].Cast<HudOverrideSystemType>();
        return hudOverrideSystemType is { IsActive: true };
    }

    private static int _getPlayersLeft()
    {
        return PlayerControl.AllPlayerControls.ToArray()
            .Count(pc => pc is { Data: { IsDead: false, Disconnected: false } });
    }

    private static bool _camsBlocked()
    {
        return CustomSettings.EnableInfosNerf && _getPlayersLeft() > CustomSettings.MaxPlayersToUseCameras;
    }

    private static bool _vitalsBlocked()
    {
        return CustomSettings.EnableInfosNerf && _getPlayersLeft() > CustomSettings.MaxPlayersToUseVitals;
    }

    private static bool _adminBlocked()
    {
        return CustomSettings.EnableInfosNerf && _getPlayersLeft() > CustomSettings.MaxPlayersToUseAdmin;
    }

    [HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin))]
    public static class ExileControllerBegin
    {
        public static void Postfix(ExileController __instance, GameData.PlayerInfo? exiled)
        {
            foreach (var role in CustomRole.Roles)
            {
                role.HookOnMeetingEnd(__instance, exiled);
            }
        }
    }

    [HarmonyPatch(typeof(VitalsMinigame), nameof(VitalsMinigame.Update))]
    public static class VitalsMinigameUpdate
    {
        public static bool Prefix(VitalsMinigame __instance)
        {
            if (UpdateVitals(__instance) == CustomRole.HookResult.ReturnFalse) return false;
            foreach (var role in CustomRole.Roles)
            {
                switch (role.HookOnVitalsUpdated(__instance))
                {
                    case CustomRole.HookResult.Continue:
                        continue;
                    case CustomRole.HookResult.Stop:
                        break;
                    case CustomRole.HookResult.ReturnTrue:
                        return true;
                    case CustomRole.HookResult.ReturnFalse:
                        return false;
                    default:
                        continue;
                }
            }

            return true;
        }

        public static CustomRole.HookResult UpdateVitals(VitalsMinigame vitalsInstance)
        {
            vitalsInstance.SabText.color = Color.white;

            if (IsCommsActive())
            {
                vitalsInstance.SabText.text = "[ C O M M S  D I S A B L E D ]";
                vitalsInstance.SabText.SetFaceColor(Palette.ImpostorRed);
            }
            else
            {
                vitalsInstance.SabText.text = "[ V I T A L S  D I S A B L E D ]\n\nabove " +
                                              (int) CustomSettings.MaxPlayersToUseVitals + " players";
                vitalsInstance.SabText.SetFaceColor(new Color32(255, 200, 0, byte.MaxValue));
            }

            if (!vitalsInstance.SabText.isActiveAndEnabled && _vitalsBlocked())
            {
                vitalsInstance.SabText.gameObject.SetActive(true);
                foreach (var vitals in vitalsInstance.vitals)
                {
                    vitals.gameObject.SetActive(false);
                }
            }

            return _vitalsBlocked() ? CustomRole.HookResult.ReturnFalse : CustomRole.HookResult.Continue;
        }
    }

    [HarmonyPatch(typeof(PlanetSurveillanceMinigame), nameof(PlanetSurveillanceMinigame.Update))]
    public static class PlanetSurveillanceMinigameUpdate
    {
        public static bool Prefix(PlanetSurveillanceMinigame __instance)
        {
            if (UpdateCameras(__instance) == CustomRole.HookResult.ReturnFalse) return false;
            foreach (var role in CustomRole.Roles)
            {
                switch (role.HookOnPlanetCameraUpdated(__instance))
                {
                    case CustomRole.HookResult.Continue:
                        continue;
                    case CustomRole.HookResult.Stop:
                        break;
                    case CustomRole.HookResult.ReturnTrue:
                        return true;
                    case CustomRole.HookResult.ReturnFalse:
                        return false;
                    default:
                        continue;
                }
            }

            return true;
        }

        private static CustomRole.HookResult UpdateCameras(PlanetSurveillanceMinigame cameras)
        {
            cameras.SabText.color = Color.white;
            if (IsCommsActive())
            {
                cameras.SabText.text = "[ C O M M S  D I S A B L E D ]";
                cameras.SabText.SetFaceColor(Palette.ImpostorRed);
            }
            else
            {
                cameras.SabText.text = "[ C A M S  D I S A B L E D ]\n\nabove " +
                                       (int) CustomSettings.MaxPlayersToUseCameras + " players";
                cameras.SabText.SetFaceColor(new Color32(255, 200, 0, byte.MaxValue));
            }

            //Toggle ON/OFF depending on maxPlayersToAllowCameras parameter
            if (!cameras.isStatic && _camsBlocked())
            {
                cameras.isStatic = true;
                cameras.ViewPort.sharedMaterial = cameras.StaticMaterial;
                cameras.SabText.gameObject.SetActive(true);
            }

            return _camsBlocked() ? CustomRole.HookResult.ReturnFalse : CustomRole.HookResult.Continue;
        }
    }

    [HarmonyPatch(typeof(PlanetSurveillanceMinigame), nameof(PlanetSurveillanceMinigame.NextCamera))]
    public static class PlanetSurveillanceMinigameNextCamera
    {
        public static bool Prefix(PlanetSurveillanceMinigame __instance, int direction)
        {
            if (UpdateNextCamera(__instance, direction) == CustomRole.HookResult.ReturnFalse) return false;
            foreach (var role in CustomRole.Roles)
            {
                switch (role.HookOnPlanetCameraNextUpdated(__instance, direction))
                {
                    case CustomRole.HookResult.Continue:
                        continue;
                    case CustomRole.HookResult.Stop:
                        break;
                    case CustomRole.HookResult.ReturnTrue:
                        return true;
                    case CustomRole.HookResult.ReturnFalse:
                        return false;
                    default:
                        continue;
                }
            }
            return true;
        }

        private static CustomRole.HookResult UpdateNextCamera(PlanetSurveillanceMinigame minigame, int direction)
        {
            if (!_camsBlocked()) return CustomRole.HookResult.Continue;

            if (direction != 0 && Constants.ShouldPlaySfx())
            {
                SoundManager.Instance.PlaySound(minigame.ChangeSound, false, 1f);
            }

            minigame.Dots[minigame.currentCamera].sprite = minigame.DotDisabled;
            minigame.currentCamera = (minigame.currentCamera + direction).Wrap(minigame.survCameras.Length);
            minigame.Dots[minigame.currentCamera].sprite = minigame.DotEnabled;
            var survCamera = minigame.survCameras[minigame.currentCamera];
            minigame.Camera.transform.position =
                survCamera.transform.position + minigame.survCameras[minigame.currentCamera].Offset;
            minigame.LocationName.text = survCamera.CamName;
            return CustomRole.HookResult.ReturnFalse;
        }
    }

    [HarmonyPatch(typeof(SurveillanceMinigame), nameof(SurveillanceMinigame.Update))]
    public static class SurveillanceMinigameUpdate
    {
        public static bool Prefix(SurveillanceMinigame __instance)
        {
            if (UpdateCamerasView(__instance) == CustomRole.HookResult.ReturnFalse) return false;
            foreach (var role in CustomRole.Roles)
            {
                switch (role.HookOnCameraUpdated(__instance))
                {
                    case CustomRole.HookResult.Continue:
                        continue;
                    case CustomRole.HookResult.Stop:
                        break;
                    case CustomRole.HookResult.ReturnTrue:
                        return true;
                    case CustomRole.HookResult.ReturnFalse:
                        return false;
                    default:
                        continue;
                }
            }

            return true;
        }

        public static CustomRole.HookResult UpdateCamerasView(SurveillanceMinigame cameras)
        {
            for (var j = 0; j < cameras.ViewPorts.Length; j++)
            {
                cameras.SabText[j].color = Color.white;
                if (IsCommsActive())
                {
                    cameras.SabText[j].text = "[ C O M M S  D I S A B L E D ]";
                    cameras.SabText[j].SetFaceColor(Palette.ImpostorRed);
                }
                else
                {
                    cameras.SabText[j].text = "[ C A M S  D I S A B L E D ]\n\nabove " +
                                              (int) CustomSettings.MaxPlayersToUseCameras + " players";
                    cameras.SabText[j].SetFaceColor(new Color32(255, 200, 0, byte.MaxValue));
                }
            }

            if (cameras.isStatic || !_camsBlocked())
                return _camsBlocked() ? CustomRole.HookResult.ReturnFalse : CustomRole.HookResult.Continue;
            cameras.isStatic = true;
            for (var j = 0; j < cameras.ViewPorts.Length; j++)
            {
                cameras.ViewPorts[j].sharedMaterial = cameras.StaticMaterial;
                cameras.SabText[j].gameObject.SetActive(true);
            }

            return _camsBlocked() ? CustomRole.HookResult.ReturnFalse : CustomRole.HookResult.Continue;
        }
    }

    [HarmonyPatch(typeof(MapCountOverlay), nameof(MapCountOverlay.Update))]
    public class MapCountOverlayUpdatePatch
    {
        public static bool Prefix(MapCountOverlay __instance)
        {
            if (UpdateAdminOverlay(__instance) == CustomRole.HookResult.ReturnFalse) return false;

            foreach (var role in CustomRole.Roles)
            {
                switch (role.HookOnAdminTableOpened(__instance))
                {
                    case CustomRole.HookResult.Continue:
                        continue;
                    case CustomRole.HookResult.Stop:
                        break;
                    case CustomRole.HookResult.ReturnTrue:
                        return true;
                    case CustomRole.HookResult.ReturnFalse:
                        return false;
                    default:
                        continue;
                }
            }

            foreach (var counterArea in __instance.CountAreas)
            {
                if (!IsCommsActive() && MapUtilities.CachedShipStatus != null)
                {
                    var plainShipRoom = MapUtilities.CachedShipStatus.FastRooms[counterArea.RoomType];

                    if (plainShipRoom != null && plainShipRoom.roomArea)
                    {
                        var num = plainShipRoom.roomArea.OverlapCollider(__instance.filter, __instance.buffer);
                        var num2 = num;
                        for (var j = 0; j < num; j++)
                        {
                            var collider2D = __instance.buffer[j];
                            if (collider2D.tag != "DeadBody")
                            {
                                var component = collider2D.GetComponent<PlayerControl>();
                                if (!component || component.Data == null || component.Data.Disconnected ||
                                    component.Data.IsDead)
                                {
                                    num2--;
                                }
                            }
                        }

                        counterArea.UpdateCount(num2);
                    }
                    else
                    {
                        Debug.LogWarning("Couldn't find counter for:" + counterArea.RoomType);
                    }
                }
                else
                {
                    counterArea.UpdateCount(0);
                }
            }

            return false;
        }

        private static CustomRole.HookResult UpdateAdminOverlay(MapCountOverlay adminTable)
        {
            adminTable.SabotageText.color = Color.white;
            var commsActive = IsCommsActive();
            if ((!adminTable.isSab && commsActive) || _adminBlocked())
            {
                adminTable.isSab = true;
                adminTable.SabotageText.gameObject.SetActive(true);
                if (commsActive)
                {
                    adminTable.SabotageText.text = "[ C O M M S  D I S A B L E D ]";
                    adminTable.SabotageText.SetFaceColor(Palette.ImpostorRed);
                    adminTable.BackgroundColor.SetColor(Palette.DisabledGrey);
                }
                else
                {
                    adminTable.SabotageText.text = "[ A D M I N  D I S A B L E D ]\n\nabove " +
                                                   (int) CustomSettings.MaxPlayersToUseAdmin + " players";
                    adminTable.SabotageText.SetFaceColor(new Color32(255, 200, 0, byte.MaxValue));
                    adminTable.BackgroundColor.SetColor(Palette.Black);
                }

                return CustomRole.HookResult.ReturnFalse;
            }

            if (!adminTable.isSab || commsActive) return CustomRole.HookResult.Continue;

            adminTable.isSab = false;
            adminTable.BackgroundColor.SetColor(Color.green);
            adminTable.SabotageText.gameObject.SetActive(false);
            return CustomRole.HookResult.Continue;
        }
    }
}