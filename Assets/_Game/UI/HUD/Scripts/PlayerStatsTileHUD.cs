using UnityEngine;
using UnityEngine.UI;

public class PlayerStatsTileHUD : MonoBehaviour
{
    [Header("Tile Groups")]
    [SerializeField] private Transform healthGroup;
    [SerializeField] private Transform staminaGroup;
    [SerializeField] private Transform manaGroup;

    [Header("Tile Prefabs")]
    [SerializeField] private GameObject healthTile;
    [SerializeField] private GameObject staminaTile;
    [SerializeField] private GameObject manaTile;
    [SerializeField] private GameObject emptyTile;

    private PlayerStatsManager statsManager;
    private int lastHealthTileCount = -1;
    private int lastHealthFilledCount = -1;
    private int lastStaminaTileCount = -1;
    private int lastStaminaFilledCount = -1;
    private int lastManaTileCount = -1;
    private int lastManaFilledCount = -1;

    public void SetPartyHudActive(bool active)
    {
        if (healthGroup != null) healthGroup.gameObject.SetActive(!active);
        if (manaGroup != null) manaGroup.gameObject.SetActive(!active);
        if (staminaGroup != null) staminaGroup.gameObject.SetActive(!active);
    }

    private void Start()
    {
        statsManager = FindAnyObjectByType<PlayerController>()?.GetComponent<PlayerStatsManager>();
        RefreshTiles(force: true);
    }

    private void Update()
    {
        if (statsManager == null)
            statsManager = FindAnyObjectByType<PlayerController>()?.GetComponent<PlayerStatsManager>();

        RefreshTiles(force: false);
    }

    private void RefreshTiles(bool force)
    {
        if (statsManager == null)
            return;

        PlayerStatsData stats = statsManager.GetPlayerStats();
        if (stats == null)
            return;

        UpdateGroup(healthGroup, healthTile, stats.maxHealth, stats.currentHealth,
            force, ref lastHealthTileCount, ref lastHealthFilledCount);
        UpdateGroup(staminaGroup, staminaTile, stats.maxStamina, stats.currentStamina,
            force, ref lastStaminaTileCount, ref lastStaminaFilledCount);
        UpdateGroup(manaGroup, manaTile, stats.maxMana, stats.currentMana,
            force, ref lastManaTileCount, ref lastManaFilledCount);
    }

    private void UpdateGroup(Transform group, GameObject filledTile, float maximum, float current,
        bool force, ref int previousTileCount, ref int previousFilledCount)
    {
        if (group == null || filledTile == null || emptyTile == null)
            return;

        int tileCount = Mathf.CeilToInt(Mathf.Max(0f, maximum));
        int filledCount = Mathf.CeilToInt(Mathf.Clamp(current, 0f, maximum) - 0.0001f);
        if (!force && tileCount == previousTileCount && filledCount == previousFilledCount)
            return;

        RawImage fullAppearance = filledTile.GetComponent<RawImage>();
        RawImage emptyAppearance = emptyTile.GetComponent<RawImage>();
        if (fullAppearance == null || emptyAppearance == null)
        {
            Debug.LogError("Stats HUD tiles require a RawImage component.", this);
            return;
        }

        while (group.childCount < tileCount)
        {
            Instantiate(filledTile, group, false);
        }

        for (int i = 0; i < group.childCount; i++)
        {
            GameObject tile = group.GetChild(i).gameObject;
            tile.SetActive(i < tileCount);
            if (i >= tileCount)
                continue;

            RawImage graphic = tile.GetComponent<RawImage>();
            if (graphic == null)
                continue;

            RawImage appearance = i < filledCount ? fullAppearance : emptyAppearance;
            graphic.color = appearance.color;
            graphic.texture = appearance.texture;
            graphic.uvRect = appearance.uvRect;
            graphic.material = appearance.material;
            graphic.raycastTarget = false;
            tile.name = $"{(i < filledCount ? filledTile.name : emptyTile.name)} {i + 1}";
        }

        previousTileCount = tileCount;
        previousFilledCount = filledCount;
    }

}
