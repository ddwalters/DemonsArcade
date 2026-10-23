using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Binds the scene-authored HUD to the persistent party roster.
public sealed class PartyHud : MonoBehaviour
{
    [SerializeField] private PartyHudView view;
    private InventoryManager inventory;
    private PartyManager party;
    private PlayerController player;
    private PlayerStatsTileHUD[] legacyHuds;
    private readonly List<PartyMemberHudRow> rows = new();

    private void Start()
    {
        inventory = FindAnyObjectByType<InventoryManager>();
        party = inventory != null ? inventory.Party : null;
        if (view == null || party == null)
        {
            Debug.LogError("Assign the scene HUD view and ensure an InventoryManager is present.", this);
            enabled = false;
            return;
        }
        party.PartyChanged += Rebuild;
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshScene();
        Rebuild();
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RefreshScene();
    private void RefreshScene()
    {
        player = FindAnyObjectByType<PlayerController>();
        legacyHuds = FindObjectsByType<PlayerStatsTileHUD>();
    }
    private void Rebuild()
    {
        foreach (var row in rows)
        {
            row.gameObject.SetActive(false);
            Destroy(row.gameObject);
        }
        rows.Clear();
        foreach (string id in party.MemberIds)
        {
            var row = view.AddRow();
            row.Bind(party.Get(id), inventory);
            rows.Add(row);
        }
    }
    private void Update()
    {
        if (view == null) return;
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        view.SetVisible(player != null);
        if (legacyHuds != null)
            foreach (var legacyHud in legacyHuds)
                if (legacyHud != null) legacyHud.SetPartyHudActive(player != null);
        foreach (var row in rows) row.Refresh();
    }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (party != null) party.PartyChanged -= Rebuild;
        foreach (var legacyHud in FindObjectsByType<PlayerStatsTileHUD>()) legacyHud.SetPartyHudActive(false);
    }
}
