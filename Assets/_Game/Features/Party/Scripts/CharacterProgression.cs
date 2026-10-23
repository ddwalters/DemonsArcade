using UnityEngine;

public static class CharacterProgression
{
    public static void GainExperience(PlayerStatsData stats, int amount)
    {
        if (stats == null || amount <= 0 || stats.lvl >= 100) return;
        stats.currentXP = (int)System.Math.Min(int.MaxValue, (long)stats.currentXP + amount);
        while (stats.lvl < 100 && stats.currentXP >= Mathf.Max(1, stats.nextLevelXP))
        {
            int threshold = Mathf.Max(1, stats.nextLevelXP);
            stats.currentXP -= threshold;
            stats.lvl++;
            stats.maxHealth += stats.lvl / 10f;
            stats.maxStamina += Mathf.RoundToInt(stats.lvl * 1.35f) / 10f;
            stats.maxMana += Mathf.RoundToInt(stats.lvl * 0.35f) / 20f;
            stats.currentHealth = stats.maxHealth;
            stats.currentStamina = stats.maxStamina;
            stats.currentMana = stats.maxMana;
            stats.nextLevelXP = (int)System.Math.Min(int.MaxValue, (long)threshold * 3);
        }
    }
}
