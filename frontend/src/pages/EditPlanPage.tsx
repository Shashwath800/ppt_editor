import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { usePipelineStore } from '../store/pipelineStore';
import { api } from '../services/api';
import type { ActionCommand } from '../types';

const actionColors: Record<string, string> = {
  rewrite_text: '#3b82f6', increase_font_size: '#8b5cf6', decrease_font_size: '#8b5cf6',
  add_conclusion_slide: '#10b981', add_slide: '#10b981', remove_slide: '#f43f5e',
  rearrange: '#f59e0b', fix_spacing: '#06b6d4', add_image: '#ec4899',
  change_layout: '#f97316', simplify: '#14b8a6',
};

export default function EditPlanPage() {
  const navigate = useNavigate();
  const { sessionId, editActions, setEditActions, setModifiedPresentation } = usePipelineStore();
  const [loading, setLoading] = useState(false);
  const [applying, setApplying] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [prompt, setPrompt] = useState<string>('');

  const generatePlan = async () => {
    if (!sessionId) return;
    if (!prompt.trim()) {
      setError('Please provide an edit instruction.');
      return;
    }
    setLoading(true);
    setError(null);
    try {
      const actions = await api.generateEditPlan(sessionId, prompt);
      setEditActions(actions.map((a: ActionCommand) => ({ ...a, approved: true })));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to generate plan');
    } finally {
      setLoading(false);
    }
  };

  const toggleAction = (index: number) => {
    const updated = [...editActions];
    updated[index] = { ...updated[index], approved: !updated[index].approved };
    setEditActions(updated);
  };

  const approveAll = () => setEditActions(editActions.map((a) => ({ ...a, approved: true })));
  const rejectAll = () => setEditActions(editActions.map((a) => ({ ...a, approved: false })));

  const applyEdits = async () => {
    if (!sessionId) return;
    setApplying(true);
    setError(null);
    try {
      const approved = editActions.filter((a) => a.approved);
      const res = await api.applyEdits(sessionId, approved);
      setModifiedPresentation(res.presentation);
      navigate('/diff-viewer');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to apply edits');
    } finally {
      setApplying(false);
    }
  };

  const approvedCount = editActions.filter((a) => a.approved).length;

  if (!sessionId) return <NoSession />;

  return (
    <div className="animate-fade-in">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold gradient-text mb-1">Edit Plan</h1>
          <p className="text-sm text-[var(--color-text-muted)]">AI-generated structured edit actions — review before execution</p>
        </div>
      </div>

      {editActions.length === 0 && !loading && (
        <div className="glass-card p-12 text-center max-w-2xl mx-auto">
          <div className="text-6xl mb-4">✏️</div>
          <h2 className="text-xl font-bold text-white mb-2">Instructions</h2>
          <p className="text-[var(--color-text-muted)] mb-6">
            Describe how you want to modify the presentation. The AI will generate structured edit actions.
          </p>
          <div className="mb-6">
            <textarea
              className="w-full h-32 p-4 bg-[rgba(15,23,42,0.6)] border border-[rgba(99,102,241,0.3)] rounded-lg text-white focus:outline-none focus:border-[var(--color-primary)] transition-colors resize-none placeholder-[var(--color-text-dim)]"
              placeholder="e.g. Rename the first node in slide 2 to 'Auth API' and add a connection to a new node 'Cache Service'..."
              value={prompt}
              onChange={(e) => setPrompt(e.target.value)}
            />
          </div>
          <button 
            className="btn-primary text-base px-8 py-3 w-full" 
            onClick={generatePlan}
            disabled={!prompt.trim()}
          >
            ⚡ Generate Plan
          </button>
        </div>
      )}

      {loading && (
        <div className="glass-card p-12 text-center">
          <div className="w-16 h-16 border-4 border-[rgba(124,58,237,0.3)] border-t-[var(--color-primary)] rounded-full animate-spin mx-auto mb-4" />
          <p className="text-[var(--color-text-muted)]">Generating edit plan...</p>
        </div>
      )}

      {error && (
        <div className="glass-card border-[var(--color-error)] p-4 text-[var(--color-error)] text-sm mb-4">
          ❌ {error}
        </div>
      )}

      {editActions.length > 0 && (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <div className="flex gap-2">
              <button className="btn-secondary" onClick={approveAll}>✅ Approve All</button>
              <button className="btn-secondary" onClick={rejectAll}>❌ Reject All</button>
              <button className="btn-secondary" onClick={generatePlan}>🔄 Regenerate</button>
            </div>
            <span className="text-sm text-[var(--color-text-muted)]">
              {approvedCount}/{editActions.length} approved
            </span>
          </div>

          <div className="grid gap-3">
            {editActions.map((action, i) => (
              <div
                key={i}
                className={`glass-card p-4 flex items-center gap-4 transition-all animate-slide-up ${
                  !action.approved ? 'opacity-50' : ''
                }`}
                style={{ animationDelay: `${i * 0.05}s` }}
              >
                <button
                  onClick={() => toggleAction(i)}
                  className={`w-8 h-8 rounded-lg flex items-center justify-center text-lg transition-all ${
                    action.approved
                      ? 'bg-[rgba(16,185,129,0.2)] text-[var(--color-success)]'
                      : 'bg-[rgba(244,63,94,0.2)] text-[var(--color-error)]'
                  }`}
                >
                  {action.approved ? '✓' : '✗'}
                </button>

                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-1">
                    <span
                      className="badge text-[10px]"
                      style={{
                        background: `${actionColors[action.action] || '#6366f1'}22`,
                        color: actionColors[action.action] || '#a78bfa',
                        border: `1px solid ${actionColors[action.action] || '#6366f1'}44`,
                      }}
                    >
                      {action.action}
                    </span>
                    {action.slide !== undefined && action.slide !== null && (
                      <span className="text-xs text-[var(--color-text-dim)]">Slide {action.slide}</span>
                    )}
                  </div>
                  <p className="text-sm text-[var(--color-text)]">{action.description}</p>
                </div>
              </div>
            ))}
          </div>

          <div className="flex justify-end pt-4">
            <button
              className="btn-primary text-base px-8 py-3"
              onClick={applyEdits}
              disabled={applying || approvedCount === 0}
            >
              {applying ? (
                <span className="flex items-center gap-2">
                  <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                  Applying...
                </span>
              ) : (
                `🚀 Apply ${approvedCount} Edit${approvedCount !== 1 ? 's' : ''}`
              )}
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
      <div className="text-5xl mb-4">✏️</div>
      <h2 className="text-xl font-bold text-white mb-2">No Presentation Loaded</h2>
      <p className="text-[var(--color-text-muted)]">Upload a .pptx file first</p>
    </div>
  );
}
