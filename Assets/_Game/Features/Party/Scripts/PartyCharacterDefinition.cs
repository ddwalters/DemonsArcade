using UnityEngine;

// Put reusable definitions under a Resources/Party folder so saves can resolve them by ID.
[CreateAssetMenu(menuName = "Game/Character Definition")]
public sealed class PartyCharacterDefinition : ScriptableObject
{
    [SerializeField, HideInInspector] private string characterId;
    public string CharacterId => characterId;
    public bool isPlayer;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(characterId))
            characterId = isPlayer ? PartyManager.PlayerId : System.Guid.NewGuid().ToString("N");
    }

    public string ValidationError
    {
        get
        {
            if (string.IsNullOrWhiteSpace(CharacterId)) return "A permanent gameplay ID is required.";
            if (isPlayer != (CharacterId == PartyManager.PlayerId)) return "The player role must match the reserved player gameplay ID.";
            var database = NodeTree.DialogOwnerDatabaseSO.Load();
            if (database == null || string.IsNullOrWhiteSpace(dialogueOwnerId) || DialogueOwner == null)
                return "Select an existing Node Tree character.";
            int ownerCount = 0;
            foreach (var owner in database.Owners)
                if (owner != null && owner.Id == dialogueOwnerId) ownerCount++;
            if (ownerCount != 1) return "Node Tree contains a duplicate character ID.";
            if (startingStats == null || startingStats.lvl < 1) return "Valid starting stats are required.";
            foreach (var other in Resources.LoadAll<PartyCharacterDefinition>("Party"))
            {
                if (other == this) continue;
                if (other.CharacterId == CharacterId) return "Another game definition uses this gameplay ID.";
                if (other.dialogueOwnerId == dialogueOwnerId) return "This Node Tree character is already linked to another game definition.";
            }
            return null;
        }
    }
    [Tooltip("The existing character in Node Tree's Dialog Owners database.")]
    public string dialogueOwnerId;
    public NodeTree.DialogOwner DialogueOwner => PartyCharacterIdentity.Resolve(dialogueOwnerId);
    public string DisplayName => DialogueOwner?.DisplayName ?? "Unknown character";
    public GameObject previewModel;
    public PlayerStatsData startingStats = new();

    public static PartyCharacterDefinition Resolve(string id)
    {
        PartyCharacterDefinition match = null;
        foreach (var definition in Resources.LoadAll<PartyCharacterDefinition>("Party"))
        {
            if (definition.CharacterId != id) continue;
            if (match != null) throw new System.InvalidOperationException("Duplicate character definition: " + id);
            match = definition;
        }
        return match;
    }
}

// Read identity and default artwork from Node Tree; never copy mood portraits into party data.
public static class PartyCharacterIdentity
{
    private static NodeTree.DialogOwnerDatabaseSO database;
    public static NodeTree.DialogOwner Resolve(string ownerId)
    {
        if (database == null) database = NodeTree.DialogOwnerDatabaseSO.Load();
        return database != null ? database.GetOwner(ownerId) : null;
    }
}
