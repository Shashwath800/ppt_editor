import { useState, useEffect, useRef } from 'react';
import { usePipelineStore } from '../store/pipelineStore';
import { api } from '../services/api';

function computeDiff(original: string, modified: string) {
  const origLines = original.split('\n');
  const modLines = modified.split('\n');
  const maxLen = Math.max(origLines.length, modLines.length);
  const lines: { left: string; right: string; type: 'unchanged' | 'added' | 'removed' | 'modified' }[] = [];
  let additions = 0, deletions = 0, modifications = 0;

  for (let i = 0; i < maxLen; i++) {
    const left = origLines[i] ?? '';
    const right = modLines[i] ?? '';
    if (left === right) {
      lines.push({ left, right, type: 'unchanged' });
    } else if (!left && right) {
      lines.push({ left: '', right, type: 'added' });
      additions++;
    } else if (left && !right) {
      lines.push({ left, right: '', type: 'removed' });
      deletions++;
    } else {
      lines.push({ left, right, type: 'modified' });
      modifications++;
    }
  }
  return { lines, additions, deletions, modifications };
}

export default function DiffViewerPage() {
  const { sessionId, semanticPresentation, modifiedPresentation } = usePipelineStore();
  const [loading, setLoading] = useState(false);
  const [original, setOriginal] = useState<string>('');
  const [modified, setModified] = useState<string>('');
  const leftRef = useRef<HTMLDivElement>(null);
  const rightRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (semanticPresentation) {
      setOriginal(JSON.stringify(semanticPresentation, null, 2));
    }
    if (modifiedPresentation) {
      setModified(JSON.stringify(modifiedPresentation, null, 2));
    } else if (sessionId && !modifiedPresentation) {
      setLoading(true);
      api.getDiff(sessionId)
        .then((diff) => {
          setOriginal(JSON.stringify(diff.original, null, 2));
          setModified(JSON.stringify(diff.modified, null, 2));
        })
        .catch(() => {})
        .finally(() => setLoading(false));
    }
  }, [sessionId, semanticPresentation, modifiedPresentation]);

  const syncScroll = (source: 'left' | 'right') => {
    const from = source === 'left' ? leftRef.current : rightRef.current;
    const to = source === 'left' ? rightRef.current : leftRef.current;
    if (from && to) {
      to.scrollTop = from.scrollTop;
    }
  };

  if (!sessionId) return <NoSession />;

  const diff = computeDiff(original, modified);

  const bgColor = (type: string, side: 'left' | 'right') => {
    if (type === 'removed' && side === 'left') return 'rgba(244,63,94,0.1)';
    if (type === 'added' && side === 'right') return 'rgba(16,185,129,0.1)';
    if (type === 'modified') return side === 'left' ? 'rgba(244,63,94,0.08)' : 'rgba(16,185,129,0.08)';
    return 'transparent';
  };

  const textColor = (type: string, side: 'left' | 'right') => {
    if (type === 'removed' && side === 'left') return '#fb7185';
    if (type === 'added' && side === 'right') return '#34d399';
    if (type === 'modified') return side === 'left' ? '#fb7185' : '#34d399';
    return 'var(--color-text-muted)';
  };

  return (
    <div className="animate-fade-in">
      <div className="mb-6">
        <h1 className="text-2xl font-bold gradient-text mb-1">Diff Viewer</h1>
        <p className="text-sm text-[var(--color-text-muted)]">Compare original vs modified semantic JSON side-by-side</p>
      </div>

      {loading ? <Spinner /> : (
        <>
          <div className="flex gap-4 mb-4">
            <span className="badge badge-error">− {diff.deletions} deletions</span>
            <span className="badge badge-success">+ {diff.additions} additions</span>
            <span className="badge badge-warning">~ {diff.modifications} modifications</span>
            <span className="badge badge-info">{diff.lines.length} total lines</span>
          </div>

          {(!original && !modified) ? (
            <div className="glass-card p-12 text-center">
              <div className="text-5xl mb-4">🔀</div>
              <h2 className="text-xl font-bold text-white mb-2">No Diff Available</h2>
              <p className="text-[var(--color-text-muted)]">Apply edits first to see the diff</p>
            </div>
          ) : (
            <div className="grid grid-cols-2 gap-0 glass-card overflow-hidden">
              <div className="border-b border-[rgba(99,102,241,0.15)] px-4 py-2 bg-[rgba(244,63,94,0.05)]">
                <span className="text-xs font-semibold text-[#fb7185]">📄 Original JSON</span>
              </div>
              <div className="border-b border-l border-[rgba(99,102,241,0.15)] px-4 py-2 bg-[rgba(16,185,129,0.05)]">
                <span className="text-xs font-semibold text-[#34d399]">📄 Modified JSON</span>
              </div>
              <div ref={leftRef} className="overflow-auto max-h-[600px] font-[var(--font-mono)] text-xs" onScroll={() => syncScroll('left')}>
                {diff.lines.map((line, i) => (
                  <div key={i} className="flex" style={{ background: bgColor(line.type, 'left') }}>
                    <span className="w-10 text-right pr-2 text-[var(--color-text-dim)] select-none border-r border-[rgba(99,102,241,0.1)] py-0.5 text-[10px]">{i + 1}</span>
                    <span className="pl-2 py-0.5 whitespace-pre" style={{ color: textColor(line.type, 'left') }}>
                      {line.type === 'removed' || line.type === 'modified' ? '−' : ' '} {line.left}
                    </span>
                  </div>
                ))}
              </div>
              <div ref={rightRef} className="overflow-auto max-h-[600px] font-[var(--font-mono)] text-xs border-l border-[rgba(99,102,241,0.15)]" onScroll={() => syncScroll('right')}>
                {diff.lines.map((line, i) => (
                  <div key={i} className="flex" style={{ background: bgColor(line.type, 'right') }}>
                    <span className="w-10 text-right pr-2 text-[var(--color-text-dim)] select-none border-r border-[rgba(99,102,241,0.1)] py-0.5 text-[10px]">{i + 1}</span>
                    <span className="pl-2 py-0.5 whitespace-pre" style={{ color: textColor(line.type, 'right') }}>
                      {line.type === 'added' || line.type === 'modified' ? '+' : ' '} {line.right}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}

function NoSession() {
  return (
    <div className="flex flex-col items-center justify-center min-h-[60vh] text-center">
      <div className="text-5xl mb-4">🔀</div>
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
