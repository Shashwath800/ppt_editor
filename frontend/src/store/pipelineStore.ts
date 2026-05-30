import { create } from 'zustand';
import type {
  PipelineState,
  OpenXmlInfo,
  SemanticPresentation,
  AnalysisResult,
  ActionCommand,
} from '../types';

interface PipelineStore {
  sessionId: string | null;
  pipelineState: PipelineState | null;
  openXmlInfo: OpenXmlInfo | null;
  semanticPresentation: SemanticPresentation | null;
  analysisResult: AnalysisResult | null;
  editActions: ActionCommand[];
  modifiedPresentation: SemanticPresentation | null;
  isLoading: boolean;
  error: string | null;
  isSidebarOpen: boolean;

  setSessionId: (id: string) => void;
  setPipelineState: (state: PipelineState) => void;
  setOpenXmlInfo: (info: OpenXmlInfo) => void;
  setSemanticPresentation: (pres: SemanticPresentation) => void;
  setAnalysisResult: (result: AnalysisResult) => void;
  setEditActions: (actions: ActionCommand[]) => void;
  setModifiedPresentation: (pres: SemanticPresentation) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  toggleSidebar: () => void;
  reset: () => void;
}

const initialState = {
  sessionId: null,
  pipelineState: null,
  openXmlInfo: null,
  semanticPresentation: null,
  analysisResult: null,
  editActions: [],
  modifiedPresentation: null,
  isLoading: false,
  error: null,
  isSidebarOpen: true,
};

export const usePipelineStore = create<PipelineStore>((set) => ({
  ...initialState,
  setSessionId: (id) => set({ sessionId: id }),
  setPipelineState: (state) => set({ pipelineState: state }),
  setOpenXmlInfo: (info) => set({ openXmlInfo: info }),
  setSemanticPresentation: (pres) => set({ semanticPresentation: pres }),
  setAnalysisResult: (result) => set({ analysisResult: result }),
  setEditActions: (actions) => set({ editActions: actions }),
  setModifiedPresentation: (pres) => set({ modifiedPresentation: pres }),
  setLoading: (loading) => set({ isLoading: loading }),
  setError: (error) => set({ error }),
  toggleSidebar: () => set((state) => ({ isSidebarOpen: !state.isSidebarOpen })),
  reset: () => set(initialState),
}));
