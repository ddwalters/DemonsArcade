using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StatsHudChecks
{
    [MenuItem("Tools/Player/Check Stats HUD %#&h")]
    public static void Run()
    {
        const string path = "Assets/_Game/UI/HUD/Prefabs/PlayerStats.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            GameObject player = new GameObject("Temporary stats check");
            SceneManager.MoveGameObjectToScene(player, root.scene);
            PlayerStatsManager stats = player.AddComponent<PlayerStatsManager>();
            PlayerStatsTileHUD hud = root.GetComponent<PlayerStatsTileHUD>();
            if (hud == null)
                throw new Exception("HUD component is missing.");

            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(PlayerStatsTileHUD).GetField("statsManager", flags).SetValue(hud, stats);
            MethodInfo refresh = typeof(PlayerStatsTileHUD).GetMethod("RefreshTiles", flags);
            Action update = () => refresh.Invoke(hud, new object[] { true });
            update();
            Check(root, "HealthGroup", 10, 10, "HealthTile");
            Check(root, "StaminaGroup", 10, 10, "StaminaTile");
            Check(root, "ManaGroup", 5, 5, "ManaTile");

            stats.SetStaminaUse(true);
            stats.UseStamina(3f);
            stats.DamagePlayer(2f);
            if (!stats.UseMana(2f))
                throw new Exception("Mana consumption failed.");
            update();
            Check(root, "HealthGroup", 10, 8, "HealthTile");
            Check(root, "StaminaGroup", 10, 7, "StaminaTile");
            Check(root, "ManaGroup", 5, 3, "ManaTile");

            var data = stats.GetPlayerStats();
            data.currentHealth = data.maxHealth;
            data.currentStamina = data.maxStamina;
            data.currentMana = data.maxMana;
            update();
            Check(root, "HealthGroup", 10, 10, "HealthTile");
            Check(root, "StaminaGroup", 10, 10, "StaminaTile");
            Check(root, "ManaGroup", 5, 5, "ManaTile");
            Debug.Log("STATS_HUD_CHECKS PASS: full 10/10/5, consumption 8/7/3, refill 10/10/5; colored and gray tile appearance verified.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void Check(GameObject root, string groupName, int total, int filled, string prefabName)
    {
        Transform group = root.transform.Find("VerticalGroup/" + groupName);
        var full = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/UI/HUD/Prefabs/" + prefabName + ".prefab").GetComponent<RawImage>();
        var empty = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/UI/HUD/Prefabs/EmptyTile.prefab").GetComponent<RawImage>();
        if (group.childCount != total)
            throw new Exception(groupName + " has " + group.childCount + " tiles; expected " + total);
        for (int i = 0; i < total; i++)
        {
            var graphic = group.GetChild(i).GetComponent<RawImage>();
            Color expected = i < filled ? full.color : empty.color;
            if (!graphic.gameObject.activeSelf || graphic.color != expected)
                throw new Exception(groupName + " tile " + i + " has incorrect visibility or color.");
        }
    }
}
