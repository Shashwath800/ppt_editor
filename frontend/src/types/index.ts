export interface SemanticElement {
  id: string;
  type: string;
  label: string;
  text: string;
  x: number;
  y: number;
  width: number;
  height: number;
  fontSize: number;
  fontColor: string;
  fillColor: string;
  imageBase64?: string;
  rotation?: number;
  zIndex?: number;
  properties: Record<string, string>;
}

export interface SemanticRelationship {
  from: string;
  to: string;
  label: string;
  type: string;
}

export interface SemanticSlide {
  id: number;
  title: string;
  classification: string;
  classificationType: string;
  elements: SemanticElement[];
  relationships: SemanticRelationship[];
  graph?: SemanticGraph;
}

export interface SemanticGraph {
  id: string;
  graphType: string;
  flowDirection: string;
  confidence: number;
  nodes: SemanticGraphNode[];
  edges: SemanticGraphEdge[];
}

export interface SemanticGraphNode {
  id: string;
  label: string;
  x: number;
  y: number;
  width: number;
  height: number;
  nodeType: string;
  properties: Record<string, string>;
}

export interface SemanticGraphEdge {
  from: string;
  to: string;
  label: string;
  edgeType: string;
  confidence: number;
}

export interface SemanticPresentation {
  fileName: string;
  slideCount: number;
  slideWidth: number;
  slideHeight: number;
  slides: SemanticSlide[];
}

export interface OpenXmlShapeInfo {
  shapeId: string;
  name: string;
  type: string;
  text: string;
  rawXml: string;
}

export interface OpenXmlRelationshipInfo {
  id: string;
  type: string;
  target: string;
}

export interface OpenXmlSlideInfo {
  slideIndex: number;
  rawXml: string;
  shapes: OpenXmlShapeInfo[];
  relationships: OpenXmlRelationshipInfo[];
}

export interface OpenXmlInfo {
  fileName: string;
  slides: OpenXmlSlideInfo[];
  slideWidth: number;
  slideHeight: number;
}

export interface AnalysisResult {
  analysis: string[];
  summary: string;
}

export interface ActionCommand {
  action: string;
  slide?: number;
  target?: string;
  value?: string;
  description: string;
  reason?: string;
  confidence?: number;
  parameters: Record<string, unknown>;
  approved: boolean;
  appliedAt?: string;
  result?: string;
}

export interface ValidationResult {
  isValid: boolean;
  errors: ValidationError[];
  warnings: ValidationWarning[];
  validatedAt?: string;
  summary?: string;
}

export interface ValidationError {
  code: string;
  message: string;
  target: string;
  severity: string;
  slide?: number;
}

export interface ValidationWarning {
  code: string;
  message: string;
  target: string;
  slide?: number;
}

export interface SemanticDocumentVersion {
  version: number;
  timestamp: string;
  description: string;
  snapshot: SemanticPresentation;
  appliedActions?: ActionCommand[];
  changedSlides?: number[];
}

export interface VersionHistory {
  sessionId: string;
  versions: SemanticDocumentVersion[];
  currentVersion: number;
}

export type PipelineStage =
  | 'upload'
  | 'openXmlParsing'
  | 'astBuilding'
  | 'semanticTree'
  | 'semanticGraph'
  | 'semanticJson'
  | 'aiAnalysis'
  | 'editPlan'
  | 'jsonTransformation'
  | 'validation'
  | 'rendering'
  | 'complete';

export type StageStatus = 'pending' | 'inProgress' | 'completed' | 'error';

export interface PipelineState {
  sessionId: string;
  fileName: string;
  currentStage: string;
  stages: Record<string, string>;
  createdAt: string;
  updatedAt: string;
}

export interface DiffResult {
  original: SemanticPresentation;
  modified: SemanticPresentation;
}
