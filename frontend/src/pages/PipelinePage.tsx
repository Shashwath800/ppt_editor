import { useNavigate } from 'react-router-dom';
import { usePipelineStore } from '../store/pipelineStore';

const stages = [
  { key: 'upload', label: 'Uploaded PPTX', icon: '📤', route: '/', desc: 'Original file stored' },
  { key: 'openXmlParsing', label: 'OpenXML', icon: '📄', route: '/openxml-viewer', desc: 'Raw XML extracted' },
  { key: 'semanticTree', label: 'Semantic Tree', icon: '🌳', route: '/semantic-tree', desc: 'Structure identified' },
  { key: 'semanticJson', label: 'Semantic JSON', icon: '📋', route: '/json-viewer', desc: 'Clean JSON generated' },
  { key: 'editPlan', label: 'Edit Plan', icon: '✏️', route: '/edit-plan', desc: 'Actions planned' },
  { key: 'jsonTransformation', label: 'Modified JSON', icon: '🔀', route: '/diff-viewer', desc: 'Edits applied to JSON' },
  { key: 'rendering', label: 'Renderer', icon: '🔧', route: '/renderer', desc: 'JSON → PPTX conversion' },
  { key: 'complete', label: 'Output PPTX', icon: '📥', route: '/renderer', desc: 'Ready to download' },
];

const statusColors: Record<string, { bg: string; text: string; border: string }> = {
  pending: { bg: 'rgba(100,116,139,0.1)', text: '#94a3b8', border: 'rgba(100,116,139,0.2)' },
  inProgress: { bg: 'rgba(124,58,237,0.1)', text: '#a78bfa', border: 'rgba(124,58,237,0.3)' },
  completed: { bg: 'rgba(16,185,129,0.1)', text: '#34d399', border: 'rgba(16,185,129,0.3)' },
  error: { bg: 'rgba(244,63,94,0.1)', text: '#fb7185', border: 'rgba(244,63,94,0.3)' },
};

export default function PipelinePage() {
  const navigate = useNavigate();
  const { sessionId, pipelineState } = usePipelineStore();

  const getStatus = (key: string): string => {
    if (!pipelineState?.stages) return 'pending';
    return pipelineState.stages[key] || 'pending';
  };

  const completedCount = stages.filter((s) => getStatus(s.key) === 'completed').length;
  const progress = Math.round((completedCount / stages.length) * 100);

  return (
    <div className="animate-fade-in">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold gradient-text mb-1">Pipeline Viewer</h1>
          <p className="text-sm text-[var(--color-text-muted)]">Visual representation of the complete processing pipeline</p>
        </div>
        {sessionId && (
          <div className="flex items-center gap-3">
            <div className="w-32 h-2 rounded-full bg-[rgba(99,102,241,0.15)] overflow-hidden">
              <div
                className="h-full rounded-full bg-gradient-to-r from-[var(--color-primary)] to-[var(--color-accent)] transition-all duration-500"
                style={{ width: `${progress}%` }}
              />
            </div>
            <span className="text-sm text-[var(--color-text-muted)]">{progress}%</span>
          </div>
        )}
      </div>

      {!sessionId ? (
        <div className="glass-card p-12 text-center">
          <div className="text-5xl mb-4">🔄</div>
          <h2 className="text-xl font-bold text-white mb-2">No Active Pipeline</h2>
          <p className="text-[var(--color-text-muted)] mb-6">Upload a .pptx file to start the pipeline</p>
          <button className="btn-primary" onClick={() => navigate('/')}>📤 Upload File</button>
        </div>
      ) : (
        <div className="flex flex-col items-center gap-2">
          {stages.map((stage, i) => {
            const status = getStatus(stage.key);
            const colors = statusColors[status] || statusColors.pending;
            return (
              <div key={stage.key} className="w-full max-w-lg">
                <button
                  onClick={() => navigate(stage.route)}
                  className="w-full glass-card p-4 flex items-center gap-4 transition-all hover:scale-[1.02] hover:border-[var(--color-primary)] animate-slide-up text-left"
                  style={{
                    animationDelay: `${i * 0.06}s`,
                    borderColor: colors.border,
                    background: colors.bg,
                  }}
                >
                  <span className="text-2xl">{stage.icon}</span>
                  <div className="flex-1">
                    <h3 className="text-sm font-semibold text-white">{stage.label}</h3>
                    <p className="text-xs text-[var(--color-text-dim)]">{stage.desc}</p>
                  </div>
                  <span
                    className="text-[10px] font-semibold uppercase px-2.5 py-1 rounded-full"
                    style={{
                      background: colors.bg,
                      color: colors.text,
                      border: `1px solid ${colors.border}`,
                    }}
                  >
                    {status === 'inProgress' ? '⏳ Running' : status === 'completed' ? '✅ Done' : status === 'error' ? '❌ Error' : '⬜ Pending'}
                  </span>
                </button>
                {i < stages.length - 1 && (
                  <div className="flex justify-center py-1">
                    <div className="w-px h-6 bg-gradient-to-b from-[rgba(124,58,237,0.4)] to-[rgba(59,130,246,0.2)]" />
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}

      <div className="mt-8 glass-card p-5">
        <h3 className="text-xs font-semibold text-[var(--color-text-dim)] uppercase tracking-wider mb-3">Architecture Rule</h3>
        <div className="flex items-center gap-3">
          <span className="text-lg">🛡️</span>
          <p className="text-sm text-[var(--color-text-muted)]">
            <strong className="text-white">AI never touches raw XML.</strong> The AI layer operates exclusively on semantic JSON. 
            The renderer is deterministic — no AI involved in JSON → PPTX conversion.
          </p>
        </div>
      </div>
    </div>
  );
}
