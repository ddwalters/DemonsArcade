using UnityEngine;

// Scene binding only. Identity and appearance come from the linked definition/Node Tree.
[RequireComponent(typeof(PlayerStatsManager), typeof(PlayerEquipment), typeof(EquipmentVisualController))]
public sealed class PartyMemberActor : MonoBehaviour
{
    [SerializeField] private PartyCharacterDefinition definition;
    private PartyManager boundParty;
    private CharacterRecord boundMember;

    private void Start() => Bind();
    private void Update()
    {
        if (boundParty != PartyManager.Current) Bind();
    }
    public void RefreshBinding() => Bind();

    private bool Bind()
    {
        if (PartyManager.Current == null || definition == null || definition.isPlayer ||
            definition.ValidationError != null) return false;

        var member = PartyManager.Current.GetOrCreate(definition.CharacterId);
        if (member.Actor != null && member.Actor != gameObject)
        {
            Debug.LogError("Duplicate party character ID: " + definition.CharacterId, this);
            return false;
        }
        if (boundMember != null && boundMember != member && boundMember.Actor == gameObject)
            boundMember.Actor = null;

        boundParty = PartyManager.Current;
        boundMember = member;
        member.Actor = gameObject;
        member.Definition = definition;
        GetComponent<PlayerStatsManager>().BindRecord(member);
        FindAnyObjectByType<InventoryManager>()?.RebuildMemberEquipment(member.Id);
        return true;
    }
    private void OnDestroy()
    {
        if (boundMember != null && boundMember.Actor == gameObject) boundMember.Actor = null;
    }
    [ContextMenu("Party/Join")]
    private void Join()
    {
        if (Bind()) boundParty.TryAdd(boundMember.Id);
    }
    [ContextMenu("Party/Leave")]
    private void Leave()
    {
        if (boundParty != null && boundMember != null) boundParty.TryRemove(boundMember.Id);
    }
}
