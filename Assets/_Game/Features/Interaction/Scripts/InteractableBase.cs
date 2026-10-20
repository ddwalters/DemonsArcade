using UnityEngine;

public class InteractableBase : MonoBehaviour, IInteractable
{
    [Header("InteractableSettings")]
    [SerializeField] private float holdDuration;
    public float HoldDuration => holdDuration;

    [SerializeField] private bool holdInteract;
    public bool HoldInteract => holdInteract;

    [SerializeField] private bool isInteractable;
    public bool IsInteractable => isInteractable;

    [SerializeField] private string tooltipMessage = "Interact";
    public string TooltipMessage => tooltipMessage;

    public virtual void OnInteract() { }

    protected void SetInteractable(bool interactable) => isInteractable = interactable;

    protected void SetToolTip(string Str) => tooltipMessage = Str;
}
