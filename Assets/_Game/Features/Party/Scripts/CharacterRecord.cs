using System;
using System.Collections.Generic;
using UnityEngine;

// Session data survives scene actors. Dismissal changes membership, never ownership.
public sealed class CharacterRecord
{
    public string Id { get; }
    private NodeTree.DialogOwner DialogueOwner => Definition != null ? Definition.DialogueOwner : PartyCharacterIdentity.Resolve(Id);
    public string Name => IsPlayer
        ? SaveManager.Instance?.ActivePlaythrough?.characterName ?? DialogueOwner?.DisplayName ?? "Player"
        : DialogueOwner?.DisplayName ?? "Unknown character";
    public Sprite Portrait => DialogueOwner?.GetPortrait(null);
    public PartyCharacterDefinition Definition { get; internal set; }
    public GameObject Actor { get; internal set; }
    public PlayerStatsData Stats { get; internal set; } = new();
    public EquipmentTotals Equipment { get; internal set; } = EquipmentCalculator.Calculate(Array.Empty<ItemData>());
    public int Gold { get; set; }
    public bool IsPlayer => Id == PartyManager.PlayerId;
    private readonly List<ItemInstance>[] containers = new List<ItemInstance>[10];

    public CharacterRecord(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A character needs a stable ID.");
        Id = id;
        for (int i = 0; i < containers.Length; i++) containers[i] = new List<ItemInstance>();
    }

    public List<ItemInstance> GetItems(int slot) => containers[slot];
    public void GainExperience(int amount) => CharacterProgression.GainExperience(Stats, amount);
}

