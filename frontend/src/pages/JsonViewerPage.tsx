import { useState, useEffect, useCallback } from 'react';
import { usePipelineStore } from '../store/pipelineStore';
import { api } from '../services/api';

function syntaxHighlight(json: string): string {
  return json
    .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
    .replace(/"(\\u[a-fA-F0-9]{4}|\\[^u]|[^\\"])*"(\s*:)?/g, (match) => {
      let cls = 'color:#10b981'; // string
      if (/:\s*$/.test(match)) cls = 'color:#7c3aed;font-weight:600'; // key
      else if (/true|false/.test(match)) cls = 'color:#f59e0b'; // bool
      else if (/null/.test(match)) cls = 'color:#f43f5e'; // null
      return `<span style="${cls}">${match}</span>`;
    })
    .replace(/\b(-?\d+\.?\d*)\b/g, '<span style="color:#06b6d4">$1</span>');
}

export default function JsonViewerPage() {
  const { sessionId, semanticPresentation, setSemanticPresentation } = usePipelineStore();
  const [loading, setLoading] = useState(false);
  const [editMode, setEditMode] = useState(false);
  const [editText, setEditText] = useState('');
  const [jsonError, setJsonError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    if (sessionId && !semanticPresentation) {
      setLoading(true);
      api.getSemanticJson(sessionId).then(setSemanticPresentation).finally(() => setLoading(false));
    }
  }, [sessionId, semanticPresentation, setSemanticPresentation]);

  const jsonString = semanticPresentation ? JSON.stringify(semanticPresentation, null, 2) : '';

  const handleEdit = useCallback(() => {
    setEditMode(true);
    setEditText(jsonString);
    setJsonError(null);
  }, [jsonString]);

  const handleSave = useCallback(async () => {
    try {
      const parsed = JSON.parse(editText);
      setJsonError(null);
      if (sessionId) {
        await api.updateSemanticJson(sessionId, parsed);
        setSemanticPresentation(parsed);
        setEditMode(false);
        setSaved(true);
        setTimeout(() => setSaved(false), 2000);
      }
    } catch {
      setJsonError('Invalid JSON format');
    }
  }, [editText, sessionId, setSemanticPresentation]);

  const copyToClipboard = useCallback(() => {
    navigator.clipboard.writeText(jsonString);
  }, [jsonString]);

  if (!sessionId) return <NoSession />;

  return (
    <div className="animate-fade-in">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold gradient-text mb-1">Semantic JSON</h1>
          <p className="text-sm text-[var(--color-text-muted)]">Clean JSON representation of the presentation</p>
        </div>
        <div className="flex gap-2">
          <button className="btn-secondary" onClick={copyToClipboard}>📋 Copy</button>
          {!editMode ? (
            <button className="btn-primary" onClick={handleEdit}>✏️ Edit</button>
          ) : (
            <>
              <button className="btn-primary" onClick={handleSave}>💾 Save</button>
              <button className="btn-secondary" onClick={() => { setEditMode(false); setJsonError(null); }}>Cancel</button>
            </>
          )}
          {saved && <span className="badge badge-success animate-fade-in">Saved!</span>}
        </div>
      </div>

      {jsonError && (
        <div className="glass-card border-[var(--color-error)] p-3 mb-4 text-[var(--color-error)] text-sm">
          ❌ {jsonError}
        </div>
      )}

      {loading ? <Spinner /> : (
        <div className="glass-card overflow-hidden">
          <div className="flex items-center justify-between px-4 py-2 border-b border-[rgba(99,102,241,0.15)] bg-[rgba(0,0,0,0.2)]">
            <span className="text-xs text-[var(--color-text-dim)]">
              {editMode ? '✏️ Edit Mode' : '👁️ View Mode'}
            </span>
            <span className="text-xs text-[var(--color-text-dim)]">
              {jsonString.split('\n').length} lines
            </span>
          </div>
          {editMode ? (
            <textarea
              value={editText}
              onChange={(e) => setEditText(e.target.value)}
              className="w-full h-[600px] p-4 bg-[rgba(0,0,0,0.3)] text-[var(--color-text)] font-[var(--font-mono)] text-xs leading-relaxed resize-none focus:outline-none"
              spellCheck={false}
            />
          ) : (
            <pre
              className="p-4 text-xs leading-relaxed overflow-auto max-h-[600px] font-[var(--font-mono)]"
              dangerouslySetInnerHTML={{ __html: syntaxHighlight(jsonString) }}
            />
          )}
        </div>
      )}
    </div>
  );
}

function NoSession() {
  return (
    <div className="flex flex-col items-center justify-center min-h-[60vh] text-center">
      <div className="text-5xl mb-4">📋</div>
      <h2 className="text-xl font-bold text-white mb-2">No Presentation Loaded</h2>
      <p className="text-[var(--color-text-muted)]">Upload a .pptx file first</p>
    </div>
  );
}

function Spinner() {
  return (
    <div className="flex items-center justify-center py-20">
      <div className="w-10 h-10 border-3 border-[rgba(124,58,237,0.3)] border-t-[var(--color-primary)] rounded-full animate-spin" />
    </div>
  );
}
