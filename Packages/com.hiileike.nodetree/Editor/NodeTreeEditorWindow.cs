using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NodeTree.Editor
{
    public class NodeTree : EditorWindow
    {
        private ListView nodeListView;
        private ToolbarSearchField nodeSearchField;
        private ListView scriptableObjectListView;
        private ToolbarSearchField scriptableObjectSearchField;
        private ListView ownerListView;
        private ToolbarSearchField ownerSearchField;
        private List<DialogOwner> filteredOwners = new List<DialogOwner>();
        private TreeGraphView mainGraphView;
        [SerializeField]
        private NodeTreeGraphSO currentGraph;

        private List<BaseNode> allNodeTypes = new List<BaseNode>();
        private List<BaseNode> filteredNodeTypes = new List<BaseNode>();
        private List<NodeTreeGraphSO> allScriptableObjects = new List<NodeTreeGraphSO>();
        private List<NodeTreeGraphSO> filteredScriptableObjects = new List<NodeTreeGraphSO>();

        public static void OpenWindow(NodeTreeGraphSO graph)
        {
            NodeTree wnd = GetWindow<NodeTree>();
            wnd.titleContent = new GUIContent(graph.name);
            wnd.minSize = new Vector2(450, 200);
            wnd.currentGraph = graph;
            if (wnd.mainGraphView != null)
                wnd.LoadGraph(graph);
        }

        public void CreateGUI()
        {
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.hiileike.nodetree/Editor/NodeTreeEditor.uxml");
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.hiileike.nodetree/Editor/NodeTreeEditor.uss");

            VisualElement visualTreeDesigner = visualTreeAsset.Instantiate();
            visualTreeDesigner.styleSheets.Add(styleSheet);
            visualTreeDesigner.style.flexGrow = 1;
            rootVisualElement.Add(visualTreeDesigner);

            nodeListView = visualTreeDesigner.Q<ListView>("node-list-view");
            nodeSearchField = visualTreeDesigner.Q<ToolbarSearchField>("node-search-field");
            scriptableObjectListView = visualTreeDesigner.Q<ListView>("scriptable-object-list-view");
            scriptableObjectSearchField = visualTreeDesigner.Q<ToolbarSearchField>("scriptable-object-search-field");
            ownerListView = visualTreeDesigner.Q<ListView>("owner-list-view");
            ownerSearchField = visualTreeDesigner.Q<ToolbarSearchField>("owner-search-field");
            mainGraphView = visualTreeDesigner.Q<TreeGraphView>("tree-graph-view");

            var toolbar = new Toolbar();
            toolbar.style.alignSelf = Align.FlexEnd;
            toolbar.style.width = new Length(100, LengthUnit.Percent);

            var currentTreeNameLabel = new Label("Welcome to my wacky node tree editor!");
            currentTreeNameLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            currentTreeNameLabel.style.paddingLeft = 3;
            currentTreeNameLabel.style.flexGrow = 1;
            toolbar.Add(currentTreeNameLabel);

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            toolbar.Add(spacer);

            var saveButton = new Button(SaveGraph) { text = "Save" };
            toolbar.Add(saveButton);

            var reloadButton = new Button(ReloadGraph) { text = "Reload" };
            toolbar.Add(reloadButton);
            rootVisualElement.Insert(0, toolbar);

            mainGraphView.StretchToParentSize();
            mainGraphView.BringToFront();

            PopulateNodes();
            PopulateScriptableObjects();
            PopulateOwners();

            visualTreeDesigner.Q<Button>("owner-add-button").clicked += () => DialogOwnerEditorWindow.Open(createNew: true);
            ownerSearchField.RegisterValueChangedCallback(evt => ApplyOwnerFilter(evt.newValue));
            DialogOwnerDatabaseUtility.Changed -= OnOwnersChanged;
            DialogOwnerDatabaseUtility.Changed += OnOwnersChanged;
            Undo.undoRedoPerformed -= OnOwnersChanged;
            Undo.undoRedoPerformed += OnOwnersChanged;

            mainGraphView.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            mainGraphView.RegisterCallback<DragPerformEvent>(OnDragPerform);
            nodeListView.RegisterCallback<PointerDownEvent>(OnListViewPointerDown);
            nodeSearchField.RegisterValueChangedCallback(OnNodeSearchChanged);
            scriptableObjectListView.RegisterCallback<PointerDownEvent>(OnScriptableObjectListViewPointerDown);
            scriptableObjectSearchField.RegisterValueChangedCallback(OnScriptableObjectSearchChanged);

            if (currentGraph != null)
                LoadGraph(currentGraph);
        }

        private void PopulateScriptableObjects(string searchTerm = "")
        {
            var scriptableObjectGuids = AssetDatabase.FindAssets("t:NodeTreeGraphSO");

            allScriptableObjects = scriptableObjectGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<NodeTreeGraphSO>(path))
                .Where(asset => asset != null)
                .OrderBy(asset => asset.name)
                .ToList();

            ApplyScriptableObjectFilter(searchTerm);

            scriptableObjectListView.makeItem = () => new Label();
            scriptableObjectListView.bindItem = (element, index) =>
            {
                (element as Label).text = filteredScriptableObjects[index].name;
            };
        }

        private void OnDisable()
        {
            DialogOwnerDatabaseUtility.Changed -= OnOwnersChanged;
            Undo.undoRedoPerformed -= OnOwnersChanged;
        }

        #region Dialog Owners
        private void OnOwnersChanged()
        {
            if (ownerListView != null)
                ApplyOwnerFilter(ownerSearchField?.value);
        }

        private void PopulateOwners()
        {
            ownerListView.fixedItemHeight = 28;
            ownerListView.selectionType = SelectionType.None;
            ownerListView.makeItem = () =>
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;

                var pfp = new Image { name = "pfp", scaleMode = ScaleMode.ScaleToFit };
                pfp.style.width = 24;
                pfp.style.height = 24;
                pfp.style.marginRight = 6;
                pfp.style.backgroundColor = new Color(0f, 0f, 0f, 0.25f);
                row.Add(pfp);

                var nameLabel = new Label { name = "owner-name" };
                nameLabel.style.flexGrow = 1;
                row.Add(nameLabel);

                row.Add(new Button { name = "owner-edit", text = "Edit" });
                return row;
            };
            ownerListView.bindItem = (element, index) =>
            {
                var owner = filteredOwners[index];
                element.Q<Image>("pfp").sprite = owner.GetPortrait(null);
                element.Q<Label>("owner-name").text = owner.DisplayName;

                var editButton = element.Q<Button>("owner-edit");
                editButton.clickable = new Clickable(() => DialogOwnerEditorWindow.Open(owner.Id));
            };

            ApplyOwnerFilter(string.Empty);
        }

        private void ApplyOwnerFilter(string searchTerm)
        {
            var db = DialogOwnerDatabaseUtility.Find();
            var owners = db != null ? db.Owners : new List<DialogOwner>();

            filteredOwners = string.IsNullOrWhiteSpace(searchTerm)
                ? owners.ToList()
                : owners.Where(o => o.DisplayName != null && o.DisplayName.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            ownerListView.itemsSource = filteredOwners;
            ownerListView.Rebuild();
        }
        #endregion

        private void ApplyScriptableObjectFilter(string searchTerm)
        {
            var normalizedSearch = searchTerm ?? string.Empty;

            filteredScriptableObjects = string.IsNullOrWhiteSpace(normalizedSearch)
                ? allScriptableObjects.ToList()
                : allScriptableObjects
                    .Where(asset => asset.name.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

            scriptableObjectListView.itemsSource = filteredScriptableObjects;
            scriptableObjectListView.Rebuild();
        }

        private void OnScriptableObjectSearchChanged(ChangeEvent<string> evt)
        {
            ApplyScriptableObjectFilter(evt.newValue);
        }

        #region Save and Load Graph
        private void SaveGraph()
        {
            if (currentGraph == null)
            {
                EditorUtility.DisplayDialog("No graph selected", "Please select a DialogueGraphSO asset to save to.", "OK");
                return;
            }

            currentGraph.Nodes.Clear();
            currentGraph.Edges.Clear();

            foreach (BaseNode node in mainGraphView.nodes)
                currentGraph.Nodes.Add(node.SaveNode());

            foreach (var edge in mainGraphView.edges)
            {
                var outputNode = edge.output.node as BaseNode;
                var inputNode = edge.input.node as BaseNode;

                if (outputNode == null || inputNode == null)
                    continue;

                var outputPorts = outputNode.outputContainer.Query<Port>().ToList();
                var outputPortIndex = outputPorts.IndexOf(edge.output);

                if (outputPortIndex < 0)
                    continue;

                currentGraph.Edges.Add(new EdgeData
                {
                    OutputNodeGuid = outputNode.GUID,
                    InputNodeGuid = inputNode.GUID,
                    OutputPortIndex = outputPortIndex
                });
            }

            EditorUtility.SetDirty(currentGraph);
            AssetDatabase.SaveAssets();
            Debug.Log($"Graph '{currentGraph.name}' saved successfully!");
        }

        private void ReloadGraph()
        {
            if (currentGraph == null)
            {
                EditorUtility.DisplayDialog("No graph selected", "Please select a DialogueGraphSO asset to reload.", "OK");
                return;
            }

            LoadGraph(currentGraph);
        }

        private void LoadGraph(NodeTreeGraphSO graph)
        {
            if (graph == null)
                return;

            currentGraph = graph;

            mainGraphView.DeleteElements(mainGraphView.graphElements);

            var createdNodes = new Dictionary<string, BaseNode>();

            if (currentGraph.Nodes == null || currentGraph.Nodes.Count == 0)
            {
                // If there are no nodes, we want to create the starting node and save.
                var newNode = (BaseNode)Activator.CreateInstance(typeof(StartNode));
                newNode.title = typeof(StartNode).Name;
                newNode.SetPosition(new Rect(position));

                mainGraphView.AddElement(newNode);
                SaveGraph();
                return;
            }

            foreach (var nodeData in currentGraph.Nodes)
            {
                BaseNode node = CreateNodeFromData(nodeData);
                if (node == null) continue;

                node.LoadNode(nodeData);

                mainGraphView.AddElement(node);
                createdNodes.Add(node.GUID, node);
            }

            foreach (var edgeData in currentGraph.Edges)
            {
                if (createdNodes.TryGetValue(edgeData.OutputNodeGuid, out var outputNode) &&
                    createdNodes.TryGetValue(edgeData.InputNodeGuid, out var inputNode))
                {
                    var outputPorts = outputNode.outputContainer.Query<Port>().ToList();
                    var outputPortIndex = edgeData.OutputPortIndex;
                    if (outputPortIndex < 0 || outputPortIndex >= outputPorts.Count)
                        outputPortIndex = 0;

                    var outputPort = outputPorts.Count > 0 ? outputPorts[outputPortIndex] : null;
                    if (outputPort == null)
                        continue;

                    var inputPort = inputNode.inputContainer.Q<Port>();

                    var newEdge = outputPort.ConnectTo(inputPort);
                    mainGraphView.AddElement(newEdge);
                }
            }
        }

        private BaseNode CreateNodeFromData(NodeData nodeData)
        {
            return NodeEditorFactory.CreateNode(nodeData);
        }

        #endregion

        #region Populate Nodes
        private void PopulateNodes()
        {
            allNodeTypes = GetEnumerableOfType<BaseNode>();
            ApplyNodeFilter(string.Empty);

            nodeListView.makeItem = () => new Label();
            nodeListView.bindItem = (element, index) =>
            {
                (element as Label).text = System.IO.Path.GetFileNameWithoutExtension(filteredNodeTypes[index].GetType().Name);
            };
        }

        private void ApplyNodeFilter(string searchTerm)
        {
            var normalizedSearch = searchTerm ?? string.Empty;

            filteredNodeTypes = string.IsNullOrWhiteSpace(normalizedSearch)
                ? allNodeTypes.ToList()
                : allNodeTypes
                    .Where(node => node.GetType().Name.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

            nodeListView.itemsSource = filteredNodeTypes;
            nodeListView.Rebuild();
        }

        private void OnNodeSearchChanged(ChangeEvent<string> evt)
        {
            ApplyNodeFilter(evt.newValue);
        }


        public static List<T> GetEnumerableOfType<T>(params object[] constructorArgs) where T : class
        {
            return GetAllSubclassesOf(typeof(T))
                .Select(type => (T)Activator.CreateInstance(type, constructorArgs))
                .ToList();
        }

        public static List<Type> GetAllSubclassesOf(Type baseType)
        {
            return Assembly.GetAssembly(baseType).GetTypes().Where(type => type.IsSubclassOf(baseType)).ToList();
        }
        #endregion

        #region Drag and Drop Nodes
        private void OnListViewPointerDown(PointerDownEvent evt)
        {
            var itemIndex = nodeListView.selectedIndex;
            if (itemIndex < 0) return;

            var selectedNodeType = (filteredNodeTypes[itemIndex])?.GetType();
            if (selectedNodeType == null) return;

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            DragAndDrop.SetGenericData("nodeTypeName", selectedNodeType.FullName);
            DragAndDrop.StartDrag(selectedNodeType.Name);
        }

        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            if (DragAndDrop.GetGenericData("nodeTypeName") != null)
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        }

        private void OnScriptableObjectListViewPointerDown(PointerDownEvent evt)
        {
            var itemIndex = scriptableObjectListView.selectedIndex;
            if (itemIndex < 0 || itemIndex >= filteredScriptableObjects.Count)
                return;

            var selectedAsset = filteredScriptableObjects[itemIndex];
            if (selectedAsset == null)
                return;

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new UnityEngine.Object[] { selectedAsset };
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            DragAndDrop.StartDrag(selectedAsset.name);
        }


        private void OnDragPerform(DragPerformEvent evt)
        {
            var nodeName = DragAndDrop.GetGenericData("nodeTypeName") as string;
            if (string.IsNullOrEmpty(nodeName)) return;

            Type nodeType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .FirstOrDefault(x => x.FullName == nodeName);

            if (nodeType == null) return;

            var newNode = (BaseNode)Activator.CreateInstance(nodeType);
            newNode.title = nodeType.Name;

            var mousePosition = mainGraphView.ChangeCoordinatesTo(mainGraphView.contentViewContainer, evt.localMousePosition);
            newNode.SetPosition(new Rect(mousePosition, Vector2.zero));

            mainGraphView.AddElement(newNode);

            DragAndDrop.AcceptDrag();
        }
        #endregion
    }
}
