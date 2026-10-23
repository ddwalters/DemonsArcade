using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NodeTree
{
    public class DialogHistoryView : MonoBehaviour
    {
        private enum Tab { Log, Conversations }

        private DialogHistory _history;
        private Canvas _canvas;
        private RectTransform _content;
        private ScrollRect _scroll;
        private Tab _tab = Tab.Log;
        private bool _built;
        private bool _dirty;

        public bool IsOpen => _canvas != null && _canvas.gameObject.activeSelf;

        public void Bind(DialogHistory history)
        {
            if (_history != null)
                _history.Changed -= MarkDirty;

            _history = history;
            _history.Changed += MarkDirty;
        }

        private void OnDestroy()
        {
            if (_history != null)
                _history.Changed -= MarkDirty;
        }

        public void Toggle()
        {
            if (IsOpen) Close(); else Open();
        }

        public void Open()
        {
            Build();
            _canvas.gameObject.SetActive(true);
            Rebuild(scrollToBottom: true);
        }

        public void Close()
        {
            if (_canvas != null)
                _canvas.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_dirty && IsOpen)
                Rebuild(scrollToBottom: false);
        }

        private void MarkDirty()
        {
            _dirty = true;
        }

        private void SetTab(Tab tab)
        {
            _tab = tab;
            Rebuild(scrollToBottom: tab == Tab.Log);
        }

        private void Rebuild(bool scrollToBottom)
        {
            _dirty = false;
            foreach (Transform child in _content)
                Destroy(child.gameObject);

            if (_tab == Tab.Log) FillLog(); else FillConversations();

            if (scrollToBottom)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
                _scroll.verticalNormalizedPosition = 0f;
            }
        }

        private void FillLog()
        {
            if (_history.Entries.Count == 0)
            {
                AddText("Nothing has been said yet.", 24, FontStyles.Italic, Color.gray);
                return;
            }

            int lastConversation = -1;
            foreach (var entry in _history.Entries)
            {
                if (entry.ConversationOrder != lastConversation)
                {
                    lastConversation = entry.ConversationOrder;
                    var record = _history.Conversations.Find(c => c.Order == lastConversation);
                    AddText($"— {record?.TreeName ?? "Unknown"} —", 22, FontStyles.Bold, new Color(1f, 1f, 1f, 0.6f), TextAlignmentOptions.Center);
                }

                string speaker = string.IsNullOrEmpty(entry.OwnerName) ? string.Empty : $"<b>{entry.OwnerName}:</b> ";
                var color = entry.IsPlayerChoice ? new Color(0.6f, 0.85f, 1f) : Color.white;
                AddText(speaker + entry.Text, 28, FontStyles.Normal, color);
            }
        }

        private void FillConversations()
        {
            if (_history.Conversations.Count == 0)
            {
                AddText("You haven't talked to anyone yet.", 24, FontStyles.Italic, Color.gray);
                return;
            }

            foreach (var c in _history.Conversations)
                AddText($"<b>{c.Order}.</b> {c.TreeName}  <alpha=#99>({c.EntryCount} lines)", 28, FontStyles.Normal, Color.white);
        }

        private void AddText(string text, float size, FontStyles style, Color color, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var go = new GameObject("Entry", typeof(RectTransform));
            go.transform.SetParent(_content, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = align;
        }

        private void Build()
        {
            if (_built) return;
            _built = true;

            var canvasGo = new GameObject("DialogHistoryCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            if (EventSystem.current == null)
                Debug.LogWarning("DialogHistoryView: no EventSystem in the scene, so the history panel can't be scrolled or clicked.");

            var dim = NewImage("Dim", canvasGo.transform, new Color(0f, 0f, 0f, 0.6f));
            Stretch(dim.rectTransform, 0f, 0f);

            var panel = NewImage("Panel", canvasGo.transform, new Color(0.08f, 0.08f, 0.1f, 0.96f));
            panel.rectTransform.anchorMin = new Vector2(0.2f, 0.1f);
            panel.rectTransform.anchorMax = new Vector2(0.8f, 0.9f);
            panel.rectTransform.offsetMin = panel.rectTransform.offsetMax = Vector2.zero;

            var header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(panel.transform, false);
            var headerRect = (RectTransform)header.transform;
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = Vector2.one;
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.sizeDelta = new Vector2(0f, 70f);
            var row = header.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(16, 16, 10, 10);
            row.spacing = 10;
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = true;

            NewButton("Log", header.transform, () => SetTab(Tab.Log));
            NewButton("Conversations", header.transform, () => SetTab(Tab.Conversations));
            NewButton("Close", header.transform, Close);

            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(RectMask2D));
            scrollGo.transform.SetParent(panel.transform, false);
            var scrollRect = (RectTransform)scrollGo.transform;
            Stretch(scrollRect, 16f, 16f);
            scrollRect.offsetMax = new Vector2(-16f, -80f);

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(scrollGo.transform, false);
            _content = (RectTransform)contentGo.transform;
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = Vector2.one;
            _content.pivot = new Vector2(0.5f, 1f);
            _content.sizeDelta = Vector2.zero;

            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14;
            layout.padding = new RectOffset(8, 24, 8, 8);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scroll = scrollGo.AddComponent<ScrollRect>();
            _scroll.content = _content;
            _scroll.horizontal = false;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = 40f;

            _canvas.gameObject.SetActive(false);
        }

        private static Image NewImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static void NewButton(string label, Transform parent, UnityEngine.Events.UnityAction onClick)
        {
            var image = NewImage(label, parent, new Color(0.2f, 0.2f, 0.26f, 1f));
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(image.transform, false);
            Stretch((RectTransform)textGo.transform, 0f, 0f);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 26;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }

        private static void Stretch(RectTransform rect, float horizontal, float vertical)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontal, vertical);
            rect.offsetMax = new Vector2(-horizontal, -vertical);
        }
    }
}
