import type { SemanticGraph } from '../../types';

interface Props {
  graph: SemanticGraph;
}

export default function GraphViewer({ graph }: Props) {
  // Calculate bounding box to scale the SVG
  const minX = Math.min(...graph.nodes.map((n) => n.x)) - 1;
  const minY = Math.min(...graph.nodes.map((n) => n.y)) - 1;
  const maxX = Math.max(...graph.nodes.map((n) => n.x + n.width)) + 1;
  const maxY = Math.max(...graph.nodes.map((n) => n.y + n.height)) + 1;

  const width = Math.max(10, maxX - minX);
  const height = Math.max(10, maxY - minY);

  // Helper to find node center for edges
  const getNodeCenter = (id: string) => {
    const node = graph.nodes.find((n) => n.id === id);
    if (!node) return { x: 0, y: 0 };
    return { x: node.x + node.width / 2, y: node.y + node.height / 2 };
  };

  return (
    <div className="relative w-full h-[500px] bg-[rgba(0,0,0,0.2)] rounded-xl border border-[rgba(99,102,241,0.2)] overflow-hidden">
      <svg
        viewBox={`${minX} ${minY} ${width} ${height}`}
        className="w-full h-full"
        style={{ filter: 'drop-shadow(0 0 8px rgba(99,102,241,0.2))' }}
      >
        <defs>
          <marker
            id="arrow"
            viewBox="0 0 10 10"
            refX="9"
            refY="5"
            markerWidth="6"
            markerHeight="6"
            orient="auto-start-reverse"
          >
            <path d="M 0 0 L 10 5 L 0 10 z" fill="var(--color-primary)" opacity="0.8" />
          </marker>
        </defs>

        {/* Draw Edges */}
        {graph.edges.map((edge, i) => {
          const from = getNodeCenter(edge.from);
          const to = getNodeCenter(edge.to);
          
          return (
            <g key={`edge-${i}`}>
              <line
                x1={from.x}
                y1={from.y}
                x2={to.x}
                y2={to.y}
                stroke={edge.confidence < 1 ? "var(--color-warning)" : "var(--color-primary)"}
                strokeWidth="0.05"
                strokeDasharray={edge.confidence < 1 ? "0.2,0.1" : "none"}
                markerEnd="url(#arrow)"
                opacity="0.8"
              />
              {edge.label && (
                <text
                  x={(from.x + to.x) / 2}
                  y={(from.y + to.y) / 2 - 0.1}
                  fontSize="0.15"
                  fill="var(--color-text-dim)"
                  textAnchor="middle"
                >
                  {edge.label}
                </text>
              )}
            </g>
          );
        })}

        {/* Draw Nodes */}
        {graph.nodes.map((node) => (
          <g key={node.id}>
            <rect
              x={node.x}
              y={node.y}
              width={node.width}
              height={node.height}
              fill="rgba(30,41,59,0.8)"
              stroke="var(--color-primary)"
              strokeWidth="0.05"
              rx="0.2"
            />
            <text
              x={node.x + node.width / 2}
              y={node.y + node.height / 2}
              fontSize="0.2"
              fill="white"
              textAnchor="middle"
              dominantBaseline="middle"
            >
              {node.label}
            </text>
            <text
              x={node.x + node.width / 2}
              y={node.y + node.height - 0.1}
              fontSize="0.12"
              fill="var(--color-text-dim)"
              textAnchor="middle"
            >
              {node.nodeType}
            </text>
          </g>
        ))}
      </svg>
    </div>
  );
}
