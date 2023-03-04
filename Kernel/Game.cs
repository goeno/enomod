﻿using EnoMod.Customs.Modules;

namespace EnoMod.Kernel;

public static class Game
{
    public enum MurderAttemptResult
    {
        PerformKill,
        SuppressKill,
    }

    public static MurderAttemptResult CheckMurderAttempt(
        PlayerControl? killer,
        PlayerControl target)
    {
        // Modified vanilla checks
        if (AmongUsClient.Instance.IsGameOver) return MurderAttemptResult.SuppressKill;
        if (killer == null || killer.Data == null || killer.Data.IsDead || killer.Data.Disconnected)
            return MurderAttemptResult.SuppressKill; // Allow non Impostor kills compared to vanilla code
        if (target == null || target.Data == null || target.Data.IsDead || target.Data.Disconnected)
            return MurderAttemptResult.SuppressKill; // Allow killing players in vents compared to vanilla code
        if (!Singleton<Shields>.Instance.ShieldFirstKilledPlayer)
            return MurderAttemptResult.PerformKill;
        if (target != killer && Singleton<Shields>.Instance.IsShielded(target))
            return MurderAttemptResult.SuppressKill;

        return MurderAttemptResult.PerformKill;
    }
}