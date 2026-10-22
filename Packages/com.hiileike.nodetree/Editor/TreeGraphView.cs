using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace NodeTree.Editor
{
    [UxmlElement]
    public partial class TreeGraphView : GraphView
    {
        public TreeGraphView()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());

            GridBackground grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();

            ports.Where(endPort =>
            endPort.node != startPort.node &&
            endPort.direction != startPort.direction &&
            endPort.portType == startPort.portType)
                .ToList()
                .ForEach(port => compatiblePorts.Add(port));

            return compatiblePorts;
        }
    }
}
