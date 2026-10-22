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

    [SerializeField] private string displayName;
    protected string ConfiguredDisplayName => displayName;
    public virtual string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName.Trim();

            const string cloneSuffix = "(Clone)";
            string objectName = gameObject.name;
            return objectName.EndsWith(cloneSuffix, System.StringComparison.Ordinal)
                ? objectName.Substring(0, objectName.Length - cloneSuffix.Length).TrimEnd()
                : objectName;
        }
    }

    public virtual void OnInteract() { }

    protected void SetInteractable(bool interactable) => isInteractable = interactable;

    protected void SetToolTip(string Str) => tooltipMessage = Str;
}
