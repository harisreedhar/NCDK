using FaulonSignatures;
using System.Collections.Generic;

namespace FaulonSignatures.EdgeColored
{
    public class EdgeColoredVertexSignature : AbstractVertexSignature
    {
        private EdgeColoredGraph graph;
        private IDictionary<string, int> colorMap;

        public EdgeColoredVertexSignature(
                int rootVertexIndex, EdgeColoredGraph graph, IDictionary<string, int> colorMap)
            : this(rootVertexIndex, -1, graph, colorMap)
        { }

        public EdgeColoredVertexSignature(
                int rootVertexIndex, int height, EdgeColoredGraph graph, IDictionary<string, int> colorMap)
                : base()
        {
            this.graph = graph;
            this.colorMap = colorMap;
            if (height == -1)
            {
                base.CreateMaximumHeight(rootVertexIndex, graph.GetVertexCount());
            }
            else
            {
                base.Create(rootVertexIndex, graph.GetVertexCount(), height);
            }
        }

        public override int[] GetConnected(int vertexIndex)
        {
            return this.graph.GetConnected(vertexIndex);
        }

        public override string GetEdgeLabel(int vertexIndex, int otherVertexIndex)
        {
            EdgeColoredGraph.Edge edge = this.graph.GetEdge(vertexIndex, otherVertexIndex);
            if (edge != null)
            {
                return edge.edgeLabel;
            }
            else
            {
                // ??
                return "";
            }
        }

        public override string GetVertexSymbol(int vertexIndex)
        {
            return ".";
        }

        public override int GetIntLabel(int vertexIndex)
        {
            return -1;
        }

        public override int ConvertEdgeLabelToColor(string label)
        {
            if (colorMap.ContainsKey(label))
            {
                return colorMap[label];
            }
            return 1;   // or throw error?
        }
    }
}
