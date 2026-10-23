using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NodeTree.Editor
{
    // Poses a model in an isolated preview scene and saves the frame as a Sprite, optionally adding it
    // straight to a Dialog Owner as a mood portrait.
    public class PortraitCaptureWindow : EditorWindow
    {
        private static readonly int[] OutputSizes = { 128, 256, 512, 1024 };
        private static readonly string[] OutputSizeLabels = { "128", "256", "512", "1024" };

        [SerializeField] private GameObject model;
        [SerializeField] private float yaw = 180f;
        [SerializeField] private float pitch = 5f;
        [SerializeField] private float viewHeight = 1f;
        [SerializeField] private Vector3 focus;
        [SerializeField] private float fieldOfView = 25f;
        [SerializeField] private Color background = new Color(0f, 0f, 0f, 0f);
        [SerializeField] private float keyIntensity = 1.2f;
        [SerializeField] private float fillIntensity = 0.5f;
        [SerializeField] private float keyYaw = -35f;
        [SerializeField] private float keyPitch = 25f;
        [SerializeField] private int sizeIndex = 1;
        [SerializeField] private string outputFolder = "Assets/Portraits";
        [SerializeField] private string ownerId;
        [SerializeField] private string mood = "Default";

        private PreviewRenderUtility _preview;
        private GameObject _instance;
        private GameObject _instanceSource;
        private Bounds _bounds;
        private Vector2 _scroll;

        [MenuItem("Tools/Node Tree/Portrait Capture")]
        public static void OpenFromMenu() => Open(null);

        public static void Open(string ownerId)
        {
            var wnd = GetWindow<PortraitCaptureWindow>("Portrait Capture");
            wnd.minSize = new Vector2(640, 420);
            if (!string.IsNullOrEmpty(ownerId))
                wnd.ownerId = ownerId;
            wnd.Show();
        }

        private void OnEnable()
        {
            _preview = new PreviewRenderUtility();
            _preview.cameraFieldOfView = fieldOfView;
            _preview.camera.nearClipPlane = 0.01f;
            _preview.camera.farClipPlane = 1000f;
            _preview.camera.clearFlags = CameraClearFlags.SolidColor;
            _instanceSource = null;
        }

        private void OnDisable()
        {
            DestroyInstance();
            _preview?.Cleanup();
            _preview = null;
        }

        private void DestroyInstance()
        {
            if (_instance != null)
                DestroyImmediate(_instance);
            _instance = null;
            _instanceSource = null;
        }

        private void EnsureInstance()
        {
            if (_instanceSource == model && (_instance != null || model == null))
                return;

            DestroyInstance();
            _instanceSource = model;
            if (model == null)
                return;

            _instance = Instantiate(model);
            _instance.hideFlags = HideFlags.HideAndDontSave;
            _instance.SetActive(true);
            _instance.transform.position = Vector3.zero;
            _instance.transform.rotation = Quaternion.identity;
            _preview.AddSingleGO(_instance);

            RecalculateBounds();
            ApplyPreset(0.25f, 0.12f);
        }

        private void RecalculateBounds()
        {
            var renderers = _instance.GetComponentsInChildren<Renderer>();
            _bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(_instance.transform.position, Vector3.one);
            foreach (var r in renderers)
                _bounds.Encapsulate(r.bounds);
        }

        // viewFraction: portion of the model height that fills the frame. topOffset: where to focus, as a fraction down from the top.
        private void ApplyPreset(float viewFraction, float topOffset)
        {
            if (_instance == null) return;

            float height = Mathf.Max(_bounds.size.y, 0.01f);
            viewHeight = height * viewFraction;
            focus = new Vector3(_bounds.center.x, _bounds.max.y - height * topOffset, _bounds.center.z);
            if (viewFraction >= 1f)
                focus.y = _bounds.center.y;
        }

        private void OnGUI()
        {
            EnsureInstance();

            using (new EditorGUILayout.HorizontalScope())
            {
                using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll, GUILayout.Width(280)))
                {
                    _scroll = scroll.scrollPosition;
                    DrawControls();
                }

                DrawPreview();
            }
        }

        private void DrawControls()
        {
            EditorGUILayout.LabelField("Model", EditorStyles.boldLabel);
            model = (GameObject)EditorGUILayout.ObjectField(model, typeof(GameObject), true);

            using (new EditorGUI.DisabledScope(_instance == null))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Framing", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Face")) ApplyPreset(0.25f, 0.12f);
                    if (GUILayout.Button("Bust")) ApplyPreset(0.5f, 0.25f);
                    if (GUILayout.Button("Full")) ApplyPreset(1.1f, 0.5f);
                }

                yaw = EditorGUILayout.Slider("Yaw", yaw, -180f, 180f);
                pitch = EditorGUILayout.Slider("Pitch", pitch, -89f, 89f);
                viewHeight = Mathf.Max(0.01f, EditorGUILayout.FloatField("View Height", viewHeight));
                focus = EditorGUILayout.Vector3Field("Focus", focus);
                fieldOfView = EditorGUILayout.Slider("Field Of View", fieldOfView, 5f, 60f);
                EditorGUILayout.HelpBox("Drag: orbit. Middle drag / Shift+drag: pan. Scroll: zoom.", MessageType.None);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Look", EditorStyles.boldLabel);
                background = EditorGUILayout.ColorField(new GUIContent("Background"), background, true, true, false);
                keyIntensity = EditorGUILayout.Slider("Key Light", keyIntensity, 0f, 3f);
                fillIntensity = EditorGUILayout.Slider("Fill Light", fillIntensity, 0f, 3f);
                keyYaw = EditorGUILayout.Slider("Key Yaw", keyYaw, -180f, 180f);
                keyPitch = EditorGUILayout.Slider("Key Pitch", keyPitch, -89f, 89f);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            sizeIndex = EditorGUILayout.Popup("Size", sizeIndex, OutputSizeLabels);
            outputFolder = EditorGUILayout.TextField("Folder", outputFolder);

            var db = DialogOwnerDatabaseUtility.Find();
            var owners = db != null ? db.Owners : null;
            if (owners != null && owners.Count > 0)
            {
                int current = Mathf.Max(0, owners.FindIndex(o => o.Id == ownerId));
                current = EditorGUILayout.Popup("Character", current, owners.Select(o => o.DisplayName).ToArray());
                ownerId = owners[current].Id;
                mood = EditorGUILayout.TextField("Mood", mood);
            }
            else
            {
                EditorGUILayout.HelpBox("No characters yet. Capture to file only, or create a character first.", MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(_instance == null))
            {
                if (GUILayout.Button("Capture To File"))
                    CaptureToFile();

                using (new EditorGUI.DisabledScope(owners == null || owners.Count == 0))
                {
                    if (GUILayout.Button("Capture And Add To Character"))
                        CaptureAndAssign(db);
                }
            }
        }

        private void DrawPreview()
        {
            var area = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            float side = Mathf.Min(area.width, area.height) - 10f;
            var rect = new Rect(area.center.x - side / 2f, area.center.y - side / 2f, side, side);

            EditorGUI.DrawRect(rect, new Color(0.22f, 0.22f, 0.22f));

            if (_instance == null)
            {
                GUI.Label(rect, "Assign a model to preview it.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            HandleInput(rect);

            if (Event.current.type != EventType.Repaint)
                return;

            _preview.BeginPreview(rect, GUIStyle.none);
            ConfigureScene();
            _preview.camera.Render();
            var texture = _preview.EndPreview();
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
        }

        private void HandleInput(Rect rect)
        {
            var e = Event.current;
            if (!rect.Contains(e.mousePosition))
                return;

            if (e.type == EventType.MouseDrag)
            {
                if (e.button == 2 || (e.button == 0 && e.shift))
                {
                    var rotation = Quaternion.Euler(pitch, yaw, 0f);
                    float scale = viewHeight / rect.height;
                    focus += (rotation * Vector3.left) * (-e.delta.x * scale) + (rotation * Vector3.up) * (e.delta.y * scale);
                }
                else if (e.button == 0)
                {
                    yaw = Mathf.Repeat(yaw + e.delta.x * 0.5f + 180f, 360f) - 180f;
                    pitch = Mathf.Clamp(pitch + e.delta.y * 0.5f, -89f, 89f);
                }

                e.Use();
                Repaint();
            }
            else if (e.type == EventType.ScrollWheel)
            {
                viewHeight = Mathf.Max(0.01f, viewHeight * (1f + e.delta.y * 0.05f));
                e.Use();
                Repaint();
            }
        }

        // Frames the focus point so viewHeight fills the (square) frame vertically.
        private void ConfigureScene()
        {
            var camera = _preview.camera;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            camera.fieldOfView = fieldOfView;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 1000f;

            float distance = viewHeight / (2f * Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad));
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            camera.transform.rotation = rotation;
            camera.transform.position = focus - rotation * Vector3.forward * distance;

            _preview.ambientColor = new Color(0.25f, 0.25f, 0.25f, 1f);

            // Lights ride along with the camera so the face is always lit from the viewer's side.
            _preview.lights[0].intensity = keyIntensity;
            _preview.lights[0].transform.rotation = rotation * Quaternion.Euler(keyPitch, keyYaw, 0f);
            _preview.lights[1].intensity = fillIntensity;
            _preview.lights[1].transform.rotation = rotation * Quaternion.Euler(-keyPitch * 0.5f, -keyYaw, 0f);
        }

        private Texture2D RenderTexture2D()
        {
            int size = OutputSizes[Mathf.Clamp(sizeIndex, 0, OutputSizes.Length - 1)];
            _preview.BeginStaticPreview(new Rect(0, 0, size, size));
            ConfigureScene();
            _preview.camera.Render();
            return _preview.EndStaticPreview();
        }

        private Sprite CaptureToFile(string baseName = null)
        {
            var texture = RenderTexture2D();
            if (texture == null)
                return null;

            Directory.CreateDirectory(outputFolder);

            baseName ??= model != null ? model.name : "Portrait";
            string fileName = SanitizeFileName(baseName) + ".png";
            string path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(outputFolder, fileName).Replace('\\', '/'));

            File.WriteAllBytes(path, texture.EncodeToPNG());
            DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            EditorGUIUtility.PingObject(sprite);
            return sprite;
        }

        private void CaptureAndAssign(DialogOwnerDatabaseSO db)
        {
            var owner = db.GetOwner(ownerId);
            if (owner == null)
                return;

            string moodName = string.IsNullOrWhiteSpace(mood) ? "Default" : mood.Trim();
            var sprite = CaptureToFile(owner.DisplayName + "_" + moodName);
            if (sprite == null)
                return;

            Undo.RecordObject(db, "Add Captured Portrait");

            var existing = owner.Portraits.FirstOrDefault(p => string.Equals(p.Mood, moodName, System.StringComparison.OrdinalIgnoreCase));
            if (existing != null)
                existing.Sprite = sprite;
            else
                owner.Portraits.Add(new OwnerPortrait { Mood = moodName, Sprite = sprite });

            if (owner.DefaultPortrait == null)
                owner.DefaultPortrait = sprite;

            DialogOwnerDatabaseUtility.Commit(db);
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
