import { useState } from 'react';
import { usePipelineStore } from '../store/pipelineStore';
import { api } from '../services/api';

export default function RendererPage() {
  const { sessionId } = usePipelineStore();
  const [rendering, setRendering] = useState(false);
  const [rendered, setRendered] = useState(false);
  const [downloading, setDownloading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleRender = async () => {
    if (!sessionId) return;
    setRendering(true);
    setError(null);
    try {
      await api.renderPptx(sessionId);
      setRendered(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Rendering failed');
    } finally {
      setRendering(false);
    }
  };

  const handleDownload = async () => {
    if (!sessionId) return;
    setDownloading(true);
    try {
      await api.downloadPptx(sessionId);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Download failed');
    } finally {
      setDownloading(false);
    }
  };

  if (!sessionId) return <NoSession />;

  return (
    <div className="animate-fade-in">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold gradient-text mb-1">Renderer</h1>
          <p className="text-sm text-[var(--color-text-muted)]">Convert semantic JSON back into a valid PPTX file</p>
        </div>
        <span className="badge badge-info">🔒 Deterministic Renderer — No AI Used</span>
      </div>

      <div className="glass-card p-4 mb-6">
        <h3 className="text-xs font-semibold text-[var(--color-text-dim)] uppercase tracking-wider mb-3">Rendering Pipeline</h3>
        <div className="flex items-center justify-around">
          {['Semantic JSON', 'Create Shapes', 'Build Slides', 'Generate PPTX'].map((step, i) => (
            <div key={step} className="flex items-center gap-3">
              <div className={`glass-card px-4 py-2 text-sm font-medium ${
                rendered ? 'text-[var(--color-success)] border-[rgba(16,185,129,0.3)]' : 'text-[var(--color-text-muted)]'
              }`}>
                {rendered ? '✅' : '⬜'} {step}
              </div>
              {i < 3 && <span className="text-[var(--color-text-dim)]">→</span>}
            </div>
          ))}
        </div>
      </div>

      {error && (
        <div className="glass-card border-[var(--color-error)] p-4 text-[var(--color-error)] text-sm mb-4">
          ❌ {error}
        </div>
      )}

      {!rendered ? (
        <div className="glass-card p-16 text-center">
          {rendering ? (
            <div>
              <div className="w-20 h-20 border-4 border-[rgba(124,58,237,0.3)] border-t-[var(--color-primary)] rounded-full animate-spin mx-auto mb-6" />
              <h2 className="text-xl font-bold text-white mb-2">Rendering PPTX...</h2>
              <p className="text-sm text-[var(--color-text-muted)]">Creating slides, placing shapes, generating layout</p>
            </div>
          ) : (
            <div>
              <div className="text-6xl mb-6">🔧</div>
              <h2 className="text-xl font-bold text-white mb-2">Ready to Render</h2>
              <p className="text-[var(--color-text-muted)] mb-8 max-w-md mx-auto">
                The renderer will deterministically convert semantic JSON back into a valid PPTX file.
              </p>
              <button className="btn-primary text-lg px-10 py-4" onClick={handleRender}>
                ⚡ Render PPTX
              </button>
            </div>
          )}
        </div>
      ) : (
        <div className="glass-card p-16 text-center animate-slide-up">
          <div className="text-6xl mb-6">🎉</div>
          <h2 className="text-2xl font-bold text-white mb-2">Rendering Complete!</h2>
          <p className="text-[var(--color-text-muted)] mb-8">Your PPTX file has been generated successfully.</p>
          <button
            className="btn-primary text-lg px-10 py-4 animate-pulse-glow"
            onClick={handleDownload}
            disabled={downloading}
          >
            {downloading ? '⏳ Downloading...' : '📥 Download PPTX'}
          </button>
          <div className="mt-6">
            <button className="btn-secondary" onClick={() => { setRendered(false); handleRender(); }}>
              🔄 Re-render
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

function NoSession() {
  return (
    <div className="flex flex-col items-center justify-center min-h-[60vh] text-center">
      <div className="text-5xl mb-4">🔧</div>
      <h2 className="text-xl font-bold text-white mb-2">No Presentation Loaded</h2>
      <p className="text-[var(--color-text-muted)]">Upload a .pptx file first</p>
    </div>
  );
}
