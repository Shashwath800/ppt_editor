import type {
  PipelineState,
  OpenXmlInfo,
  OpenXmlSlideInfo,
  SemanticPresentation,
  AnalysisResult,
  ActionCommand,
  ValidationResult,
  VersionHistory,
  DiffResult,
} from '../types';

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? '/api';

async function request<T>(url: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${url}`, {
    headers: { 'Content-Type': 'application/json', ...options?.headers },
    ...options,
  });
  if (!res.ok) {
    const text = await res.text().catch(() => 'Unknown error');
    throw new Error(`API Error ${res.status}: ${text}`);
  }
  const contentType = res.headers.get('content-type');
  if (contentType?.includes('application/json')) {
    return res.json();
  }
  return {} as T;
}

export const api = {
  async uploadFile(file: File): Promise<PipelineState> {
    const formData = new FormData();
    formData.append('file', file);
    const res = await fetch(`${API_BASE}/upload`, {
      method: 'POST',
      body: formData,
    });
    if (!res.ok) throw new Error(`Upload failed: ${res.status}`);
    return res.json();
  },

  getOpenXmlInfo(sessionId: string): Promise<OpenXmlInfo> {
    return request(`/openxml/${sessionId}`);
  },

  getOpenXmlSlide(sessionId: string, index: number): Promise<OpenXmlSlideInfo> {
    return request(`/openxml/${sessionId}/slide/${index}`);
  },

  getSemanticTree(sessionId: string): Promise<SemanticPresentation> {
    return request(`/semantic/${sessionId}/tree`);
  },

  getSemanticJson(sessionId: string): Promise<SemanticPresentation> {
    return request(`/semantic/${sessionId}/json`);
  },

  updateSemanticJson(sessionId: string, data: SemanticPresentation): Promise<void> {
    return request(`/semantic/${sessionId}/json`, {
      method: 'PUT',
      body: JSON.stringify(data),
    });
  },

  runAnalysis(sessionId: string): Promise<AnalysisResult> {
    return request(`/agent/${sessionId}/analyze`, { method: 'POST' });
  },

  generateEditPlan(sessionId: string, prompt: string): Promise<ActionCommand[]> {
    return request(`/agent/${sessionId}/edit-plan`, {
      method: 'POST',
      body: JSON.stringify({ prompt }),
    });
  },

  applyEdits(sessionId: string, actions: ActionCommand[]): Promise<{ presentation: SemanticPresentation, validation: ValidationResult, auditLog: ActionCommand[] }> {
    return request(`/agent/${sessionId}/apply-edits`, {
      method: 'POST',
      body: JSON.stringify(actions),
    });
  },

  validate(sessionId: string): Promise<ValidationResult> {
    return request(`/validation/${sessionId}/validate`, { method: 'POST' });
  },

  getVersionHistory(sessionId: string): Promise<VersionHistory> {
    return request(`/validation/${sessionId}/history`);
  },

  rollbackVersion(sessionId: string, version: number): Promise<SemanticPresentation> {
    return request(`/validation/${sessionId}/rollback/${version}`, { method: 'POST' });
  },

  async renderPptx(sessionId: string): Promise<void> {
    await request(`/renderer/${sessionId}/render`, { method: 'POST' });
  },

  async downloadPptx(sessionId: string): Promise<void> {
    const res = await fetch(`${API_BASE}/renderer/${sessionId}/download`);
    if (!res.ok) throw new Error('Download failed');
    const blob = await res.blob();
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `modified_presentation.pptx`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  },

  getPipelineState(sessionId: string): Promise<PipelineState> {
    return request(`/pipeline/${sessionId}`);
  },

  getDiff(sessionId: string): Promise<DiffResult> {
    return request(`/pipeline/${sessionId}/diff`);
  },
};
