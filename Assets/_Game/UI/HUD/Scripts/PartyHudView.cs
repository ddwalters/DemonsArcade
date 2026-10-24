using UnityEngine;

public sealed class PartyHudView : MonoBehaviour
{
    [SerializeField] private Transform rows;
    [SerializeField] private PartyMemberHudRow rowPrefab;
    public void SetVisible(bool visible) => rows.gameObject.SetActive(visible);
    public PartyMemberHudRow AddRow() => Instantiate(rowPrefab, rows);
}
