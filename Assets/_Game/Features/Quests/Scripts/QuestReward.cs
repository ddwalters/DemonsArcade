using NodeTree;
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

    private static bool rewardGrantedThisPlaySession;

    [SerializeField] private GameObject modalRoot;
    [SerializeField] private Button okButton;
    private GameObject previouslySelectedObject;

    public bool IsOpen => modalRoot != null && modalRoot.activeSelf;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionState()
    {
        rewardGrantedThisPlaySession = false;
        ConditionContext.SetBool(CompletedConditionKey, false);
    }

    private void OnEnable()
    {
        okButton.onClick.AddListener(DismissModal);
        NodeTreeEvents.Subscribe(QuestCompleteEventKey, CompleteQuest);
        ConditionContext.SetBool(CompletedConditionKey, rewardGrantedThisPlaySession);
    }

    private void OnDisable()
    {
        okButton.onClick.RemoveListener(DismissModal);
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

        modalRoot.SetActive(true);
        if (eventSystem != null)
            eventSystem.SetSelectedGameObject(okButton.gameObject);
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

        previouslySelectedObject = null;
        modalRoot.SetActive(false);
    }

}
