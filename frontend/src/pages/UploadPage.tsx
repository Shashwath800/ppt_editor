import { useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { usePipelineStore } from '../store/pipelineStore';
import { api } from '../services/api';

export default function UploadPage() {
  const navigate = useNavigate();
  const { setPipelineState, setSessionId } = usePipelineStore();
  const [isDragging, setIsDragging] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [uploadComplete, setUploadComplete] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [fileName, setFileName] = useState('');

  const handleFile = useCallback(async (file: File) => {
    if (!file.name.endsWith('.pptx')) {
      setError('Only .pptx files are accepted');
      return;
    }
    setError(null);
    setIsUploading(true);
    setFileName(file.name);
    try {
      const state = await api.uploadFile(file);
      setPipelineState(state);
      setSessionId(state.sessionId);
      setUploadComplete(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Upload failed');
    } finally {
      setIsUploading(false);
    }
  }, [setPipelineState, setSessionId]);

  const onDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
    const file = e.dataTransfer.files[0];
    if (file) handleFile(file);
  }, [handleFile]);

  const onFileInput = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) handleFile(file);
  }, [handleFile]);

  return (
    <div className="flex flex-col items-center justify-center min-h-[80vh]">
      <div className="text-center mb-10 animate-slide-up">
        <h1 className="text-4xl font-bold gradient-text mb-3">Upload Presentation</h1>
        <p className="text-[var(--color-text-muted)] text-lg">
          Drop your .pptx file to begin the semantic pipeline
        </p>
      </div>

      {!uploadComplete ? (
        <div
          className={`w-full max-w-2xl glass-card p-16 text-center cursor-pointer transition-all duration-300 animate-slide-up ${
            isDragging
              ? 'border-[var(--color-primary)] bg-[rgba(124,58,237,0.1)] scale-[1.02]'
              : 'hover:border-[rgba(99,102,241,0.4)]'
          } ${isUploading ? 'pointer-events-none' : ''}`}
          style={{ borderStyle: 'dashed', borderWidth: '2px' }}
          onDragOver={(e) => { e.preventDefault(); setIsDragging(true); }}
          onDragLeave={() => setIsDragging(false)}
          onDrop={onDrop}
          onClick={() => document.getElementById('file-input')?.click()}
        >
          <input
            id="file-input"
            type="file"
            accept=".pptx"
            className="hidden"
            onChange={onFileInput}
          />
          {isUploading ? (
            <div className="flex flex-col items-center gap-4">
              <div className="w-16 h-16 border-4 border-[rgba(124,58,237,0.3)] border-t-[var(--color-primary)] rounded-full animate-spin" />
              <p className="text-[var(--color-text-muted)]">Processing {fileName}...</p>
              <p className="text-xs text-[var(--color-text-dim)]">Parsing OpenXML → Building Semantic Tree</p>
            </div>
          ) : (
            <div className="flex flex-col items-center gap-4">
              <div className="text-6xl mb-2">{isDragging ? '📥' : '📤'}</div>
              <p className="text-lg font-medium text-[var(--color-text)]">
                {isDragging ? 'Drop file here' : 'Drag & drop your .pptx file'}
              </p>
              <p className="text-sm text-[var(--color-text-dim)]">or click to browse</p>
              <div className="badge badge-info mt-2">.pptx files only</div>
            </div>
          )}
        </div>
      ) : (
        <div className="w-full max-w-2xl glass-card p-10 text-center animate-slide-up">
          <div className="text-6xl mb-4">✅</div>
          <h2 className="text-2xl font-bold text-white mb-2">Upload Complete!</h2>
          <p className="text-[var(--color-text-muted)] mb-6">{fileName} has been parsed successfully</p>
          <div className="flex gap-4 justify-center">
            <button className="btn-primary text-base px-8 py-3" onClick={() => navigate('/pipeline')}>
              🔄 View Pipeline
            </button>
            <button className="btn-secondary" onClick={() => navigate('/openxml-viewer')}>
              📄 View OpenXML
            </button>
            <button className="btn-secondary" onClick={() => navigate('/semantic-tree')}>
              🌳 Semantic Tree
            </button>
          </div>
        </div>
      )}

      {error && (
        <div className="mt-6 glass-card border-[var(--color-error)] p-4 text-[var(--color-error)] text-sm animate-fade-in">
          ❌ {error}
        </div>
      )}

      <div className="mt-16 w-full max-w-3xl animate-fade-in" style={{ animationDelay: '0.3s' }}>
        <h3 className="text-center text-sm font-semibold text-[var(--color-text-dim)] uppercase tracking-wider mb-6">
          Pipeline Stages
        </h3>
        <div className="flex items-center justify-between gap-1">
          {['PPTX', 'OpenXML', 'Semantic Tree', 'JSON', 'AI Analysis', 'Edit Plan', 'Modified JSON', 'Renderer', 'Output'].map((stage, i) => (
            <div key={stage} className="flex items-center gap-1">
              <div className="glass-card px-2.5 py-1.5 text-[10px] font-medium text-[var(--color-text-muted)] whitespace-nowrap">
                {stage}
              </div>
              {i < 8 && <span className="text-[var(--color-text-dim)] text-xs">→</span>}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
