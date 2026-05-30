import { useEffect, useState } from 'react';
import { usePipelineStore } from '../store/pipelineStore';
import type { VersionHistory } from '../types';
import { api } from '../services/api';
import { useNavigate } from 'react-router-dom';

export default function VersionHistoryPage() {
  const { sessionId } = usePipelineStore();
  const [history, setHistory] = useState<VersionHistory | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const navigate = useNavigate();

  const loadHistory = async () => {
    if (!sessionId) return;
    setLoading(true);
    try {
      const res = await api.getVersionHistory(sessionId);
      setHistory(res);
    } catch (err: any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadHistory();
  }, [sessionId]);

  const handleRollback = async (version: number) => {
    if (!sessionId) return;
    if (!confirm(`Are you sure you want to rollback to version ${version}?`)) return;
    
    try {
      await api.rollbackVersion(sessionId, version);
      alert('Rollback successful');
      loadHistory();
      navigate('/json-viewer');
    } catch (err: any) {
      alert('Rollback failed: ' + err.message);
    }
  };

  if (!sessionId) {
    return <div className="p-8 text-[var(--color-text-dim)]">Please upload a presentation first.</div>;
  }

  return (
    <div className="p-8 max-w-5xl mx-auto animate-fade-in">
      <div className="mb-8 flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-white mb-2">Version History</h1>
          <p className="text-[var(--color-text-dim)]">
            Audit trail of semantic changes and applied AI edits.
          </p>
        </div>
        <button className="btn btn-outline" onClick={loadHistory}>Refresh</button>
      </div>

      {loading && !history ? (
        <div className="text-center p-8">Loading history...</div>
      ) : error ? (
        <div className="text-red-400 p-4 bg-red-500/10 rounded-lg">{error}</div>
      ) : (
        <div className="space-y-6 relative before:absolute before:inset-0 before:ml-5 before:-translate-x-px md:before:mx-auto md:before:translate-x-0 before:h-full before:w-0.5 before:bg-gradient-to-b before:from-transparent before:via-[rgba(99,102,241,0.2)] before:to-transparent">
          {history?.versions.slice().reverse().map((v, i) => (
            <div key={v.version} className="relative flex items-center justify-between md:justify-normal md:odd:flex-row-reverse group is-active">
              {/* Timeline dot */}
              <div className="flex items-center justify-center w-10 h-10 rounded-full border-4 border-[var(--color-bg)] bg-[var(--color-primary)] shadow shrink-0 md:order-1 md:group-odd:-translate-x-1/2 md:group-even:translate-x-1/2">
                <span className="text-white text-sm font-bold">v{v.version}</span>
              </div>
              
              {/* Card */}
              <div className="w-[calc(100%-4rem)] md:w-[calc(50%-2.5rem)] glass-card p-5">
                <div className="flex justify-between items-start mb-2">
                  <h3 className="font-semibold text-white">{v.description}</h3>
                  <span className="text-xs text-[var(--color-text-dim)]">
                    {new Date(v.timestamp).toLocaleString()}
                  </span>
                </div>
                
                {v.appliedActions && v.appliedActions.length > 0 && (
                  <div className="mt-4 pt-4 border-t border-[rgba(255,255,255,0.05)]">
                    <p className="text-xs uppercase tracking-wider text-[var(--color-text-muted)] mb-3">
                      Applied Actions
                    </p>
                    <ul className="space-y-2">
                      {v.appliedActions.map((action, j) => (
                        <li key={j} className="text-sm">
                          <span className="font-mono text-[var(--color-accent)]">{action.action}</span>
                          <span className="text-[var(--color-text-dim)] mx-2">→</span>
                          <span className="text-gray-300">{action.description}</span>
                        </li>
                      ))}
                    </ul>
                  </div>
                )}

                {i !== 0 && (
                  <div className="mt-4 text-right">
                    <button 
                      onClick={() => handleRollback(v.version)}
                      className="text-xs text-[var(--color-primary)] hover:text-white transition-colors px-3 py-1.5 rounded bg-[rgba(99,102,241,0.1)] hover:bg-[var(--color-primary)]"
                    >
                      Rollback to v{v.version}
                    </button>
                  </div>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
