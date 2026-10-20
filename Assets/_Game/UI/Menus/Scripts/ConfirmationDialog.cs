using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ConfirmationDialog : MonoBehaviour
{
    public TMP_Text mainText;
    public Button okButton;
    public Button cancelButton;
    private Action confirmed;
    private GameObject previousSelection;
    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        okButton.onClick.AddListener(Confirm);
        cancelButton.onClick.AddListener(Cancel);
    }

    public void Show(string text, Action onConfirmed)
    {
        previousSelection = EventSystem.current?.currentSelectedGameObject;
        mainText.text = text;
        confirmed = onConfirmed;
        gameObject.SetActive(true);
        cancelButton.Select();
    }

    public void Cancel()
    {
        confirmed = null;
        gameObject.SetActive(false);
        if (previousSelection != null && previousSelection.activeInHierarchy)
            EventSystem.current?.SetSelectedGameObject(previousSelection);
    }

    private void Confirm()
    {
        Action action = confirmed;
        Cancel();
        action?.Invoke();
    }
}
