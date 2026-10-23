using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps the prototype dialogue text clear of the currently active portrait and response choices.
/// The dialogue package remains responsible for choosing a speaker side and rendering its portrait.
/// </summary>
public sealed class PrototypeDialogueLayout : MonoBehaviour
{
    private enum SpeakerSide
    {
        None,
        Left,
        Right
    }

    private const float PortraitSize = 160f;
    private const float PortraitInset = 24f;
    private const float TextGap = 24f;
    private const float OptionsWidth = 380f;
    private const float OptionsGap = 24f;
    private const float DialogueTop = 60f;
    private const float DialogueBottom = 43f;
    private const float OwnerNameTop = 20f;
    private const float OwnerNameHeight = 28f;

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
        ApplyPortraitGeometry();
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

        float leftMargin = side == SpeakerSide.Left
            ? PortraitInset + PortraitSize + TextGap
            : PortraitInset;
        float rightMargin = side == SpeakerSide.Right
            ? PortraitInset + PortraitSize + TextGap
            : PortraitInset;

        if (hasOptions)
            rightMargin = OptionsWidth + OptionsGap + PortraitInset;

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
        dialogueText.anchorMin = Vector2.zero;
        dialogueText.anchorMax = Vector2.one;
        dialogueText.pivot = new Vector2(0.5f, 0.5f);
        dialogueText.offsetMin = new Vector2(left, DialogueBottom);
        dialogueText.offsetMax = new Vector2(-right, -DialogueTop);
    }

    private void SetOwnerNameMargins(float left, float right)
    {
        RectTransform nameRect = ownerNameText.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0f, 1f);
        nameRect.anchoredPosition = new Vector2(left, -OwnerNameTop);
        nameRect.sizeDelta = new Vector2(-left - right, OwnerNameHeight);
    }

    private void ApplyPortraitGeometry()
    {
        if (leftPortrait != null)
        {
            leftPortrait.anchorMin = new Vector2(0f, 0.5f);
            leftPortrait.anchorMax = new Vector2(0f, 0.5f);
            leftPortrait.pivot = new Vector2(0f, 0.5f);
            leftPortrait.anchoredPosition = new Vector2(PortraitInset, 0f);
            leftPortrait.sizeDelta = new Vector2(PortraitSize, PortraitSize);
        }

        if (rightPortrait != null)
        {
            rightPortrait.anchorMin = new Vector2(1f, 0.5f);
            rightPortrait.anchorMax = new Vector2(1f, 0.5f);
            rightPortrait.pivot = new Vector2(1f, 0.5f);
            rightPortrait.anchoredPosition = new Vector2(-PortraitInset, 0f);
            rightPortrait.sizeDelta = new Vector2(PortraitSize, PortraitSize);
        }
    }
}
