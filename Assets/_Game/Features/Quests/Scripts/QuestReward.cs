using NodeTree;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Handles the quest completion event, grants its one-time reward, and
/// displays a small blocking confirmation modal.
/// </summary>
public sealed class QuestReward : MonoBehaviour
{
    public const string QuestCompleteEventKey = "quest.alden.complete";
    public const string CompletedConditionKey = "quest.alden.completed";

    private const int RewardAmount = 10;
    private const int ModalSortingOrder = 32767;

    private static bool rewardGrantedThisPlaySession;

    private GameObject modalRoot;
    private Button okButton;
    private GameObject previouslySelectedObject;

    public bool IsOpen => modalRoot != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionState()
    {
        rewardGrantedThisPlaySession = false;
        ConditionContext.SetBool(CompletedConditionKey, false);
    }

    private void OnEnable()
    {
        NodeTreeEvents.Subscribe(QuestCompleteEventKey, CompleteQuest);
        ConditionContext.SetBool(CompletedConditionKey, rewardGrantedThisPlaySession);
    }

    private void OnDisable()
    {
        NodeTreeEvents.Unsubscribe(QuestCompleteEventKey, CompleteQuest);
        CloseModal(restoreSelection: false);
    }

    private void CompleteQuest()
    {
        if (rewardGrantedThisPlaySession)
            return;

        PlayerController currentPlayer = FindAnyObjectByType<PlayerController>();
        if (currentPlayer == null)
            return;

        currentPlayer.goldAmount += RewardAmount;
        rewardGrantedThisPlaySession = true;
        ConditionContext.SetBool(CompletedConditionKey, true);

        if (!IsOpen)
            ShowModal();
    }

    private void ShowModal()
    {
        EventSystem eventSystem = EventSystem.current;
        previouslySelectedObject = eventSystem != null
            ? eventSystem.currentSelectedGameObject
            : null;

        modalRoot = new GameObject(
            "QuestRewardModal",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = modalRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = ModalSortingOrder;

        CanvasScaler scaler = modalRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform rootRect = modalRoot.GetComponent<RectTransform>();
        StretchToParent(rootRect);

        Image blocker = modalRoot.AddComponent<Image>();
        blocker.color = new Color(0f, 0f, 0f, 0.72f);
        blocker.raycastTarget = true;

        GameObject panelObject = CreateUiObject("QuestCompletePanel", rootRect);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(600f, 320f);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0.09f, 0.12f, 0.18f, 1f);
        panelImage.raycastTarget = false;

        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font == null)
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        TextMeshProUGUI title = CreateText("Title", panelRect, font);
        title.text = "Quest complete";
        title.fontSize = 42f;
        title.fontStyle = FontStyles.Bold;
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot = new Vector2(0.5f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -38f);
        title.rectTransform.sizeDelta = new Vector2(-48f, 62f);

        TextMeshProUGUI reward = CreateText("Reward", panelRect, font);
        reward.text = "+10 Gold";
        reward.fontSize = 34f;
        reward.fontStyle = FontStyles.Bold;
        reward.color = new Color(1f, 0.82f, 0.31f, 1f);
        reward.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        reward.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        reward.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        reward.rectTransform.anchoredPosition = new Vector2(0f, 8f);
        reward.rectTransform.sizeDelta = new Vector2(420f, 58f);

        GameObject buttonObject = CreateUiObject("OKButton", panelRect);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 34f);
        buttonRect.sizeDelta = new Vector2(180f, 58f);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.22f, 0.54f, 0.83f, 1f);
        buttonImage.raycastTarget = true;

        okButton = buttonObject.AddComponent<Button>();
        okButton.targetGraphic = buttonImage;
        okButton.navigation = new Navigation { mode = Navigation.Mode.Automatic };
        okButton.onClick.AddListener(DismissModal);

        TextMeshProUGUI buttonLabel = CreateText("Label", buttonRect, font);
        buttonLabel.text = "OK";
        buttonLabel.fontSize = 25f;
        buttonLabel.fontStyle = FontStyles.Bold;
        StretchToParent(buttonLabel.rectTransform);

        if (eventSystem != null)
            eventSystem.SetSelectedGameObject(buttonObject);
    }

    public void Dismiss() => CloseModal(restoreSelection: true);

    private void DismissModal()
    {
        CloseModal(restoreSelection: true);
    }

    private void CloseModal(bool restoreSelection)
    {
        if (modalRoot == null)
            return;

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null && okButton != null &&
            eventSystem.currentSelectedGameObject == okButton.gameObject)
        {
            GameObject selection = restoreSelection && previouslySelectedObject != null
                ? previouslySelectedObject
                : null;
            eventSystem.SetSelectedGameObject(selection);
        }

        okButton = null;
        previouslySelectedObject = null;
        Destroy(modalRoot);
        modalRoot = null;
    }

    private static TextMeshProUGUI CreateText(
        string objectName,
        RectTransform parent,
        TMP_FontAsset font)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject CreateUiObject(string objectName, RectTransform parent)
    {
        GameObject child = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer));
        child.transform.SetParent(parent, false);
        return child;
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }
}
