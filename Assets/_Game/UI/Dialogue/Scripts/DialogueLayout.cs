using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps the dialogue text clear of the currently active portrait and response choices.
/// The dialogue package remains responsible for choosing a speaker side and rendering its portrait.
/// </summary>
public sealed class DialogueLayout : MonoBehaviour
{
    private enum SpeakerSide
    {
        None,
        Left,
        Right
    }

    [SerializeField, Min(0f)] private float textGap = 24f;
    [SerializeField, Min(0f)] private float optionsGap = 24f;
    private Vector2 textPadding;

    [SerializeField] private Image leftPortraitImage;
    [SerializeField] private Image rightPortraitImage;
    [SerializeField] private RectTransform leftPortrait;
    [SerializeField] private RectTransform rightPortrait;
    [SerializeField] private TextMeshProUGUI ownerNameText;
    [SerializeField] private RectTransform dialogueText;
    [SerializeField] private RectTransform responseOptions;

    private bool _hasAppliedLayout;
    private SpeakerSide _lastSide;
    private bool _lastHasPortrait;
    private bool _lastHasOptions;
    private bool _lastDialogVisible;

    private void Awake()
    {
        textPadding = new Vector2(dialogueText.offsetMin.x, -dialogueText.offsetMax.x);
        RefreshLayout();
    }

    private void LateUpdate()
    {
        RefreshLayout();
    }

    private void RefreshLayout()
    {
        if (dialogueText == null || ownerNameText == null || responseOptions == null)
            return;

        bool dialogVisible = dialogueText.gameObject.activeInHierarchy;
        bool hasSpeaker = dialogVisible && ownerNameText.gameObject.activeSelf;
        SpeakerSide side = hasSpeaker ? DetectSpeakerSide() : SpeakerSide.None;
        Image speakerImage = side == SpeakerSide.Left ? leftPortraitImage : rightPortraitImage;
        bool hasPortrait = dialogVisible
            && side != SpeakerSide.None
            && speakerImage != null
            && speakerImage.enabled
            && speakerImage.sprite != null;
        bool hasOptions = dialogVisible && responseOptions.childCount > 0;

        if (_hasAppliedLayout
            && side == _lastSide
            && hasPortrait == _lastHasPortrait
            && hasOptions == _lastHasOptions
            && dialogVisible == _lastDialogVisible)
        {
            return;
        }

        if (leftPortrait != null)
            leftPortrait.gameObject.SetActive(dialogVisible && side == SpeakerSide.Left && hasPortrait);
        if (rightPortrait != null)
            rightPortrait.gameObject.SetActive(dialogVisible && side == SpeakerSide.Right && hasPortrait);

        float leftMargin = textPadding.x;
        float rightMargin = textPadding.y;
        if (hasPortrait && side == SpeakerSide.Left)
            leftMargin = leftPortrait.anchoredPosition.x + leftPortrait.rect.width + textGap;
        if (hasPortrait && side == SpeakerSide.Right)
            rightMargin = -rightPortrait.anchoredPosition.x + rightPortrait.rect.width + textGap;
        if (hasOptions)
            rightMargin = Mathf.Max(rightMargin, responseOptions.rect.width + optionsGap + textPadding.y);

        SetDialogueMargins(leftMargin, rightMargin);
        SetOwnerNameMargins(leftMargin, rightMargin);

        _lastSide = side;
        _lastHasPortrait = hasPortrait;
        _lastHasOptions = hasOptions;
        _lastDialogVisible = dialogVisible;
        _hasAppliedLayout = true;
    }

    private SpeakerSide DetectSpeakerSide()
    {
        if (leftPortraitImage == null || rightPortraitImage == null)
            return SpeakerSide.None;

        float leftBrightness = Brightness(leftPortraitImage.color);
        float rightBrightness = Brightness(rightPortraitImage.color);

        if (Mathf.Abs(leftBrightness - rightBrightness) > 0.05f)
            return leftBrightness > rightBrightness ? SpeakerSide.Left : SpeakerSide.Right;

        bool leftHasSprite = leftPortraitImage.enabled && leftPortraitImage.sprite != null;
        bool rightHasSprite = rightPortraitImage.enabled && rightPortraitImage.sprite != null;

        if (leftHasSprite != rightHasSprite)
            return leftHasSprite ? SpeakerSide.Left : SpeakerSide.Right;

        return SpeakerSide.None;
    }

    private static float Brightness(Color color)
    {
        return (color.r + color.g + color.b) / 3f;
    }

    private void SetDialogueMargins(float left, float right)
    {
        dialogueText.offsetMin = new Vector2(left, dialogueText.offsetMin.y);
        dialogueText.offsetMax = new Vector2(-right, dialogueText.offsetMax.y);
    }

    private void SetOwnerNameMargins(float left, float right)
    {
        RectTransform nameRect = ownerNameText.rectTransform;
        nameRect.offsetMin = new Vector2(left, nameRect.offsetMin.y);
        nameRect.offsetMax = new Vector2(-right, nameRect.offsetMax.y);
    }
}
