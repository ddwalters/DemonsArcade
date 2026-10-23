using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NodeTree
{
    public class DialogRunner : MonoBehaviour
    {
        private NodeTreeGraphSO _dialogTree;
        private Action _pendingContinue;
        private bool _continueReady;

        private Coroutine _typewriterCoroutine;
        private bool _typingComplete = true;

        public static DialogRunner Instance { get; private set; }
        public bool IsDialogActive { get; private set; }

        [Header("UI References")]
        public TextMeshProUGUI defaultTextLocation;
        [SerializeField] private Transform buttonListContainer;
        [SerializeField] private GameObject dialogCanvas;

        [Header("Prefabs")]
        [Tooltip("Styled Response Option Prefab")]
        public GameObject styledButtonPrefab;

        [Header("Input")]
        public InputActionReference continueAction;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [Tooltip("Global typing tick sound. Can be overridden per node.")]
        public AudioClip typingTickClip;

        [Header("Dialog Owners (optional)")]
        [Tooltip("Shows the speaking owner's name.")]
        [SerializeField] private TextMeshProUGUI ownerNameText;
        [SerializeField] private Image leftPortraitImage;
        [SerializeField] private Image rightPortraitImage;
        [Tooltip("Tint applied to the portrait of whoever is not speaking.")]
        [SerializeField] private Color inactivePortraitTint = new Color(0.5f, 0.5f, 0.5f, 1f);
        [Tooltip("Leave empty to load Resources/NodeTree/DialogOwners.")]
        [SerializeField] private DialogOwnerDatabaseSO ownerDatabase;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (dialogCanvas != null)
                dialogCanvas.SetActive(false);
        }

        private void OnEnable()
        {
            continueAction?.action.Enable();
        }

        private void OnDisable()
        {
            continueAction?.action.Disable();
        }

        private void Update()
        {
            if (_typingComplete && !_continueReady && _pendingContinue != null)
                _continueReady = true;

            if (continueAction == null || !continueAction.action.WasPressedThisFrame())
                return;

            if (!_typingComplete)
            {
                SkipTypewriter();
                return;
            }

            if (_continueReady && _pendingContinue != null)
            {
                var callback = _pendingContinue;
                _pendingContinue = null;
                _continueReady = false;
                callback.Invoke();
            }
        }

        public void StartDialog(NodeTreeGraphSO dialogTree)
        {
            _dialogTree = dialogTree;
            if (_dialogTree.Nodes == null || _dialogTree.Nodes.Count == 0)
                return;

            foreach (var node in _dialogTree.Nodes)
                node.HasSeen = false;

            IsDialogActive = true;
            if (dialogCanvas != null) dialogCanvas.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _continueReady = false;
            ClearSpeaker();

            var startingNodeData = _dialogTree.Nodes.FirstOrDefault(n => n is StartNodeData);
            var startingNode = NodeRuntimeFactory.CreateRuntimeNode(startingNodeData);
            startingNode.Execute(this, _dialogTree);
        }

        // Shows the owner's name and mood portrait on the given side. The other side keeps its last portrait, tinted.
        // A null / unknown owner (narration) hides the name and tints both portraits.
        public void ShowSpeaker(string ownerId, string mood, PortraitSide side)
        {
            if (ownerDatabase == null)
                ownerDatabase = DialogOwnerDatabaseSO.Load();

            var owner = ownerDatabase != null ? ownerDatabase.GetOwner(ownerId) : null;

            if (ownerNameText != null)
            {
                ownerNameText.gameObject.SetActive(owner != null);
                if (owner != null)
                    ownerNameText.text = owner.DisplayName;
            }

            var speakerImage = side == PortraitSide.Left ? leftPortraitImage : rightPortraitImage;
            var otherImage = side == PortraitSide.Left ? rightPortraitImage : leftPortraitImage;

            if (otherImage != null)
                otherImage.color = inactivePortraitTint;

            if (owner == null)
            {
                if (speakerImage != null)
                    speakerImage.color = inactivePortraitTint;
                return;
            }

            if (speakerImage == null)
                return;

            var sprite = owner.GetPortrait(mood);
            speakerImage.sprite = sprite;
            speakerImage.enabled = sprite != null;
            speakerImage.color = Color.white;
        }

        private void ClearSpeaker()
        {
            if (ownerNameText != null)
                ownerNameText.gameObject.SetActive(false);

            foreach (var image in new[] { leftPortraitImage, rightPortraitImage })
            {
                if (image == null) continue;
                image.sprite = null;
                image.enabled = false;
            }
        }

        public void ApplyTextStyle(TMP_FontAsset fontAsset, float fontSize, Color fontColor, FontStyles fontStyle)
        {
            if (fontAsset != null)
                defaultTextLocation.font = fontAsset;
            if (fontSize > 0)
                defaultTextLocation.fontSize = fontSize;
            defaultTextLocation.color = fontColor;
            defaultTextLocation.fontStyle = fontStyle;
        }

        // onContinue null for nodes that advance via response buttons rather than a press.
        public void DisplayText(string text, float textSpeed, AudioClip tickClip, Action onContinue = null)
        {
            _pendingContinue = onContinue;
            _continueReady = false;
            _typingComplete = false;

            float speed = textSpeed > 0 ? textSpeed : (_dialogTree != null ? _dialogTree.defaultTextSpeed : 0.05f);
            AudioClip resolvedClip = tickClip != null ? tickClip : typingTickClip;

            if (_typewriterCoroutine != null)
                StopCoroutine(_typewriterCoroutine);

            _typewriterCoroutine = StartCoroutine(TypewriterRoutine(text, speed, resolvedClip));
        }

        private IEnumerator TypewriterRoutine(string fullText, float charDelay, AudioClip tickClip)
        {
            defaultTextLocation.text = fullText;
            defaultTextLocation.ForceMeshUpdate();

            int totalChars = defaultTextLocation.textInfo.characterCount;
            defaultTextLocation.maxVisibleCharacters = 0;

            for (int i = 1; i <= totalChars; i++)
            {
                defaultTextLocation.maxVisibleCharacters = i;

                if (tickClip != null && audioSource != null)
                    audioSource.PlayOneShot(tickClip);

                yield return new WaitForSeconds(charDelay);
            }

            _typingComplete = true;
            _typewriterCoroutine = null;
        }

        private void SkipTypewriter()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            defaultTextLocation.maxVisibleCharacters = int.MaxValue;
            _typingComplete = true;
            _continueReady = true;
        }

        // Kept for backward compatibility. Sets text instantly with no typewriter.
        public void UpdateMainText(string text)
        {
            defaultTextLocation.text = text;
            defaultTextLocation.maxVisibleCharacters = int.MaxValue;
        }

        // Kept for backward compatibility. Use DisplayText for new nodes.
        public void WaitForContinue(Action onContinue)
        {
            _pendingContinue = onContinue;
            _typingComplete = true;
            _continueReady = false;
        }

        public void ClearOptions()
        {
            foreach (Transform child in buttonListContainer)
                Destroy(child.gameObject);
        }

        public void AddResponseOption(string text, Action onOptionSelected)
        {
            GameObject newButton = Instantiate(styledButtonPrefab, buttonListContainer);
            newButton.GetComponentInChildren<TextMeshProUGUI>().text = text;

            Button buttonComp = newButton.GetComponent<Button>();
            buttonComp.onClick.AddListener(() => onOptionSelected?.Invoke());
        }

        public void GoToNode(string inputNodeGuid)
        {
            if (_dialogTree == null)
            {
                Debug.LogError("No dialog tree is currently running!");
                return;
            }

            var nextNodeData = _dialogTree.Nodes.FirstOrDefault(x => x.Guid == inputNodeGuid);

            if (nextNodeData != null)
            {
                var nextNode = NodeRuntimeFactory.CreateRuntimeNode(nextNodeData);
                nextNode.Execute(this, _dialogTree);
            }
            else
            {
                StopDialog();
            }
        }

        public void StopDialog()
        {
            Debug.Log("End of dialog reached.");

            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            _pendingContinue = null;
            _continueReady = false;
            _typingComplete = true;
            IsDialogActive = false;

            if (dialogCanvas != null) dialogCanvas.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            UpdateMainText(string.Empty);
            ClearOptions();
            ClearSpeaker();
            _dialogTree = null;
        }
    }
}
