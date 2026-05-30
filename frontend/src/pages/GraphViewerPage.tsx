import { useEffect, useState } from 'react';
import { usePipelineStore } from '../store/pipelineStore';
import type { SemanticPresentation } from '../types';
import { api } from '../services/api';
import GraphViewer from '../components/graph/GraphViewer';

export default function GraphViewerPage() {
  const { sessionId } = usePipelineStore();
  const [data, setData] = useState<SemanticPresentation | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!sessionId) return;
    setLoading(true);
    api.getSemanticJson(sessionId)
      .then(setData)
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  }, [sessionId]);

  if (!sessionId) {
    return <div className="p-8 text-[var(--color-text-dim)]">Please upload a presentation first.</div>;
  }

  if (loading) return <div className="p-8">Loading...</div>;
  if (error) return <div className="p-8 text-[var(--color-error)]">Error: {error}</div>;
  if (!data) return null;

  const graphSlides = data.slides.filter(s => s.graph);

  return (
    <div className="p-8 max-w-7xl mx-auto animate-fade-in">
      <div className="mb-8">
        <h1 className="text-3xl font-bold tracking-tight text-white mb-2">Semantic Graphs</h1>
        <p className="text-[var(--color-text-dim)]">
          Found {graphSlides.length} slides containing architecture diagrams, flowcharts, or pipelines.
        </p>
      </div>

      <div className="space-y-8">
        {graphSlides.map((slide) => (
          <div key={slide.id} className="glass-card p-6">
            <div className="flex justify-between items-center mb-6">
              <div>
                <h3 className="text-lg font-semibold text-white">Slide {slide.id}: {slide.title}</h3>
                <div className="flex gap-3 mt-2">
                  <span className="badge badge-primary">{slide.graph!.graphType}</span>
                  <span className="badge badge-outline">Flow: {slide.graph!.flowDirection}</span>
                  <span className="badge badge-outline">Confidence: {Math.round(slide.graph!.confidence * 100)}%</span>
                </div>
              </div>
            </div>
            
            <GraphViewer graph={slide.graph!} />
          </div>
        ))}

        {graphSlides.length === 0 && (
          <div className="glass-card p-8 text-center text-[var(--color-text-muted)]">
            No semantic graphs detected in this presentation.
          </div>
        )}
      </div>
    </div>
  );
}
