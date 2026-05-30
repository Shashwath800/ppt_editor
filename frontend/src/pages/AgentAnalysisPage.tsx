import { useState } from 'react';
import { usePipelineStore } from '../store/pipelineStore';
import { api } from '../services/api';

export default function AgentAnalysisPage() {
  const { sessionId, analysisResult, setAnalysisResult, semanticPresentation } = usePipelineStore();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showJson, setShowJson] = useState(false);

  const runAnalysis = async () => {
    if (!sessionId) return;
    setLoading(true);
    setError(null);
    try {
      const result = await api.runAnalysis(sessionId);
      setAnalysisResult(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Analysis failed');
    } finally {
      setLoading(false);
    }
  };

  if (!sessionId) return <NoSession />;

  return (
    <div className="animate-fade-in">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold gradient-text mb-1">AI Analysis</h1>
          <p className="text-sm text-[var(--color-text-muted)]">AI-powered presentation analysis using semantic JSON</p>
        </div>
        <div className="flex items-center gap-3">
          <span className="badge badge-info">🛡️ AI operates on Semantic JSON only — Never raw XML</span>
        </div>
      </div>

      {!analysisResult && !loading && (
        <div className="glass-card p-12 text-center">
          <div className="text-6xl mb-4">🤖</div>
          <h2 className="text-xl font-bold text-white mb-2">Ready to Analyze</h2>
          <p className="text-[var(--color-text-muted)] mb-6 max-w-md mx-auto">
            The AI will analyze your presentation's semantic JSON and provide actionable observations.
          </p>
          <button className="btn-primary text-base px-8 py-3" onClick={runAnalysis}>
            🔍 Run Analysis
          </button>
        </div>
      )}

      {loading && (
        <div className="glass-card p-12 text-center">
          <div className="w-16 h-16 border-4 border-[rgba(124,58,237,0.3)] border-t-[var(--color-primary)] rounded-full animate-spin mx-auto mb-4" />
          <h2 className="text-lg font-bold text-white mb-1">Analyzing...</h2>
          <p className="text-sm text-[var(--color-text-muted)]">AI is reviewing semantic JSON (powered by Groq)</p>
        </div>
      )}

      {error && (
        <div className="glass-card border-[var(--color-error)] p-4 text-[var(--color-error)] text-sm mb-4">
          ❌ {error}
          <button className="btn-secondary ml-4" onClick={runAnalysis}>Retry</button>
        </div>
      )}

      {analysisResult && (
        <div className="space-y-4">
          {analysisResult.summary && (
            <div className="glass-card p-5 border-l-4 border-l-[var(--color-accent)] animate-slide-up">
              <h3 className="text-sm font-semibold text-[var(--color-accent)] mb-2">📊 Summary</h3>
              <p className="text-[var(--color-text)] text-sm">{analysisResult.summary}</p>
            </div>
          )}

          <div className="grid gap-3">
            {analysisResult.analysis?.map((observation, i) => {
              const isWarning = /excessive|inconsistent|missing|error|issue|problem/i.test(observation);
              const isPositive = /good|great|well|excellent|strong/i.test(observation);
              const variant = isWarning ? 'warning' : isPositive ? 'success' : 'info';
              const icon = isWarning ? '⚠️' : isPositive ? '✅' : '💡';
              return (
                <div
                  key={i}
                  className={`glass-card p-4 animate-slide-up flex items-start gap-3`}
                  style={{ animationDelay: `${i * 0.08}s` }}
                >
                  <span className="text-lg">{icon}</span>
                  <div className="flex-1">
                    <p className="text-sm text-[var(--color-text)]">{observation}</p>
                  </div>
                  <span className={`badge badge-${variant} text-[9px]`}>
                    {variant}
                  </span>
                </div>
              );
            })}
          </div>

          <div className="flex gap-2 mt-4">
            <button className="btn-primary" onClick={runAnalysis}>🔄 Re-analyze</button>
            <button className="btn-secondary" onClick={() => setShowJson(!showJson)}>
              {showJson ? 'Hide' : 'Show'} Source JSON
            </button>
          </div>

          {showJson && semanticPresentation && (
            <div className="glass-card p-4 mt-2 animate-fade-in">
              <h3 className="text-xs font-semibold text-[var(--color-text-dim)] mb-2 uppercase tracking-wider">
                JSON sent to AI (transparency view)
              </h3>
              <pre className="text-xs leading-relaxed overflow-auto max-h-[300px] p-3 bg-[rgba(0,0,0,0.3)] rounded-lg font-[var(--font-mono)] text-[var(--color-text-muted)]">
                {JSON.stringify(semanticPresentation, null, 2)}
              </pre>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function NoSession() {
  return (
    <div className="flex flex-col items-center justify-center min-h-[60vh] text-center">
      <div className="text-5xl mb-4">🤖</div>
      <h2 className="text-xl font-bold text-white mb-2">No Presentation Loaded</h2>
      <p className="text-[var(--color-text-muted)]">Upload a .pptx file first</p>
    </div>
  );
}
