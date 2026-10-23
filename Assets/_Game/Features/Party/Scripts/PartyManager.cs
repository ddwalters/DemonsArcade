using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Session data survives scene actors. Dismissal changes membership, never ownership.
public sealed class PartyMember
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
    public bool IsPlayer => Id == PartyManager.PlayerId;
    private readonly List<ItemInstance>[] containers = new List<ItemInstance>[10];

    public PartyMember(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A character needs a stable ID.");
        Id = id;
        for (int i = 0; i < containers.Length; i++) containers[i] = new List<ItemInstance>();
    }

    public List<ItemInstance> GetItems(int slot) => containers[slot];
    internal void BindContainer(int slot, List<ItemInstance> items) => containers[slot] = items;
    public void GainExperience(int amount) => CharacterProgression.GainExperience(Stats, amount);
}

public sealed class PartyManager : MonoBehaviour
{
    public const string PlayerId = "player";
    public const int Capacity = 3;
    public static PartyManager Current { get; private set; }
    private readonly Dictionary<string, PartyMember> characters = new();
    private readonly List<string> members = new();
    private PlayerController player;
    public IReadOnlyList<string> MemberIds => members.AsReadOnly();
    public IEnumerable<PartyMember> Characters => characters.Values;
    public string LeaderId { get; private set; } = PlayerId;
    public event Action PartyChanged;

    private void Awake()
    {
        Current = this;
        GetOrCreate(PlayerId);
        members.Add(PlayerId);
    }

    private void Update()
    {
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        BindPlayer(player);
    }

    private void OnDestroy() { if (Current == this) Current = null; }

    public PartyMember Get(string id) => id != null && characters.TryGetValue(id, out var member) ? member : null;
    public PartyMember GetOrCreate(string id)
    {
        if (!characters.TryGetValue(id, out var member))
        {
            var definition = PartyCharacterDefinition.Resolve(id);
            if (definition == null || definition.ValidationError != null)
                throw new InvalidOperationException("Cannot register character " + id + ": " + (definition?.ValidationError ?? "No game character definition."));
            member = new PartyMember(id) { Definition = definition };
            member.Stats = JsonUtility.FromJson<PlayerStatsData>(JsonUtility.ToJson(definition.startingStats));
            characters.Add(id, member);
        }
        return member;
    }
    public bool Contains(string id) => members.Contains(id);
    public bool TryAdd(string id)
    {
        if (Get(id)?.Definition == null || Get(id).Definition.ValidationError != null || Contains(id) || members.Count >= Capacity) return false;
        members.Add(id);
        PartyChanged?.Invoke();
        return true;
    }
    public bool TryRemove(string id)
    {
        if (id == PlayerId || !members.Remove(id)) return false;
        if (LeaderId == id) LeaderId = PlayerId;
        PartyChanged?.Invoke();
        return true;
    }
    public bool SetLeader(string id)
    {
        if (!Contains(id)) return false;
        LeaderId = id;
        PartyChanged?.Invoke();
        return true;
    }
    public bool MoveMember(string id, int index)
    {
        if (!Contains(id) || index < 0 || index >= members.Count) return false;
        members.Remove(id);
        members.Insert(index, id);
        PartyChanged?.Invoke();
        return true;
    }

    public void BindPlayer(PlayerController player)
    {
        if (player == null) return;
        var member = Get(PlayerId);
        member.Actor = player.gameObject;
        member.Stats = player.GetComponent<PlayerStatsManager>().GetPlayerStats();
    }

    public SavedParty Capture(ItemCatalog catalog)
    {
        var result = new SavedParty { leaderId = LeaderId, memberIds = new List<string>(members) };
        foreach (PartyMember member in Characters.Where(member => !member.IsPlayer))
        {
            var saved = new SavedPartyMember { id = member.Id, stats = member.Stats };
            for (int slot = 0; slot < 10; slot++)
                saved.inventory.Add(new SavedContainer { slot = slot, items = catalog.Capture(member.GetItems(slot)) });
            result.characters.Add(saved);
        }
        return result;
    }

    public static void Validate(SavedParty saved, ItemCatalog catalog)
    {
        if (saved == null) return; // Older saves contain only the player.
        if (saved.memberIds == null || saved.characters == null || saved.memberIds.Count < 1 ||
            saved.memberIds.Count > Capacity || !saved.memberIds.Contains(PlayerId) ||
            saved.memberIds.Distinct().Count() != saved.memberIds.Count || !saved.memberIds.Contains(saved.leaderId))
            throw new ArgumentException("Invalid party roster.");
        var ids = new HashSet<string> { PlayerId };
        foreach (var member in saved.characters)
        {
            if (member == null || string.IsNullOrWhiteSpace(member.id) || !ids.Add(member.id) ||
                member.stats == null || member.stats.lvl < 1 || !SaveManager.HasValidInventoryLayout(member.inventory))
                throw new ArgumentException("Invalid party character.");
            var definition = PartyCharacterDefinition.Resolve(member.id);
            if (definition == null || definition.ValidationError != null)
                throw new ArgumentException("Missing or invalid game character definition: " + member.id);
            foreach (var container in member.inventory) catalog.ValidateContainer(container, member.stats.inventoryType);
        }
        if (saved.memberIds.Any(id => !ids.Contains(id))) throw new ArgumentException("Missing party character.");
    }

    public void Restore(SavedParty saved, ItemCatalog catalog)
    {
        Validate(saved, catalog);
        var actors = characters.Values.Where(member => !member.IsPlayer).ToList();
        foreach (var member in actors) characters.Remove(member.Id);
        members.Clear();
        members.Add(PlayerId);
        LeaderId = PlayerId;
        if (saved != null)
        {
            foreach (var data in saved.characters)
            {
                var member = GetOrCreate(data.id);
                member.Stats = data.stats;
                foreach (var container in data.inventory) member.GetItems(container.slot).AddRange(catalog.Restore(container.items));
            }
            members.Clear();
            members.AddRange(saved.memberIds);
            LeaderId = saved.leaderId;
        }
        foreach (var previous in actors)
        {
            var restored = Get(previous.Id);
            if (restored != null) { restored.Actor = previous.Actor; }
        }
        foreach (var actor in FindObjectsByType<PartyMemberActor>()) actor.RefreshBinding();
        PartyChanged?.Invoke();
    }
}

[Serializable]
public sealed class SavedParty
{
    public string leaderId = PartyManager.PlayerId;
    public List<string> memberIds = new();
    public List<SavedPartyMember> characters = new();
}
[Serializable]
public sealed class SavedPartyMember
{
    public string id;
    public PlayerStatsData stats;
    public List<SavedContainer> inventory = new();
}
