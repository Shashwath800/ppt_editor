import { useEffect, useState } from 'react';
import { usePipelineStore } from '../store/pipelineStore';
import type { ValidationResult } from '../types';
import { api } from '../services/api';

export default function ValidationPage() {
  const { sessionId } = usePipelineStore();
  const [result, setResult] = useState<ValidationResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const runValidation = async () => {
    if (!sessionId) return;
    setLoading(true);
    setError('');
    try {
      const res = await api.validate(sessionId);
      setResult(res);
    } catch (err: any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (sessionId) runValidation();
  }, [sessionId]);

  if (!sessionId) {
    return <div className="p-8 text-[var(--color-text-dim)]">Please upload a presentation first.</div>;
  }

  return (
    <div className="p-8 max-w-5xl mx-auto animate-fade-in">
      <div className="flex justify-between items-center mb-8">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-white mb-2">Pre-render Validation</h1>
          <p className="text-[var(--color-text-dim)]">
            Validates structural integrity, graph references, and geometric overlaps.
          </p>
        </div>
        <button className="btn btn-primary" onClick={runValidation} disabled={loading}>
          {loading ? 'Validating...' : 'Run Validation'}
        </button>
      </div>

      {error && (
        <div className="p-4 mb-6 rounded-lg bg-[rgba(239,68,68,0.1)] border border-[rgba(239,68,68,0.2)] text-red-400">
          {error}
        </div>
      )}

      {result && (
        <div className="space-y-6">
          <div className={`p-6 rounded-xl border ${result.isValid ? 'bg-[rgba(34,197,94,0.05)] border-green-500/20' : 'bg-[rgba(239,68,68,0.05)] border-red-500/20'}`}>
            <h2 className={`text-xl font-bold ${result.isValid ? 'text-green-400' : 'text-red-400'}`}>
              {result.summary}
            </h2>
            <p className="text-sm text-[var(--color-text-muted)] mt-1">
              Validated at: {new Date(result.validatedAt || '').toLocaleString()}
            </p>
          </div>

          {result.errors.length > 0 && (
            <div className="glass-card overflow-hidden">
              <div className="bg-[rgba(239,68,68,0.1)] p-4 border-b border-[rgba(239,68,68,0.2)]">
                <h3 className="text-red-400 font-semibold flex items-center gap-2">
                  <span>❌</span> Errors ({result.errors.length})
                </h3>
              </div>
              <div className="divide-y divide-[var(--color-border)]">
                {result.errors.map((err, i) => (
                  <div key={i} className="p-4 flex flex-col gap-1">
                    <div className="flex items-center gap-3">
                      <span className="text-xs font-mono px-2 py-0.5 rounded bg-red-500/20 text-red-300">
                        {err.code}
                      </span>
                      {err.slide && (
                        <span className="text-xs text-[var(--color-text-dim)]">Slide {err.slide}</span>
                      )}
                    </div>
                    <p className="text-white mt-1">{err.message}</p>
                    <p className="text-sm text-[var(--color-text-dim)] font-mono">Target: {err.target}</p>
                  </div>
                ))}
              </div>
            </div>
          )}

          {result.warnings.length > 0 && (
            <div className="glass-card overflow-hidden">
              <div className="bg-[rgba(245,158,11,0.1)] p-4 border-b border-[rgba(245,158,11,0.2)]">
                <h3 className="text-amber-400 font-semibold flex items-center gap-2">
                  <span>⚠️</span> Warnings ({result.warnings.length})
                </h3>
              </div>
              <div className="divide-y divide-[var(--color-border)]">
                {result.warnings.map((warn, i) => (
                  <div key={i} className="p-4 flex flex-col gap-1">
                    <div className="flex items-center gap-3">
                      <span className="text-xs font-mono px-2 py-0.5 rounded bg-amber-500/20 text-amber-300">
                        {warn.code}
                      </span>
                      {warn.slide && (
                        <span className="text-xs text-[var(--color-text-dim)]">Slide {warn.slide}</span>
                      )}
                    </div>
                    <p className="text-white mt-1">{warn.message}</p>
                    <p className="text-sm text-[var(--color-text-dim)] font-mono">Target: {warn.target}</p>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
