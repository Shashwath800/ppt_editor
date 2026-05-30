import { useState, useEffect } from 'react';
import { usePipelineStore } from '../store/pipelineStore';
import { api } from '../services/api';
import type { SemanticSlide, SemanticElement } from '../types';

const typeIcons: Record<string, string> = {
  title: '📝', subtitle: '📝', text: '📦', image: '🖼️', shape: '🔷',
  connector: '🔗', process: '⚙️', table: '📊', chart: '📈', group: '📁',
};
const classColors: Record<string, string> = {
  titleslide: '#7c3aed', bulletslide: '#3b82f6', flowchart: '#06b6d4',
  architecturediagram: '#10b981', tableslide: '#f59e0b', imageslide: '#ec4899',
  chartslide: '#8b5cf6', timeline: '#f97316', comparisonslide: '#14b8a6',
};

function TreeNode({ label, icon, badge, badgeColor, children, defaultOpen = false }: {
  label: string; icon: string; badge?: string; badgeColor?: string;
  children?: React.ReactNode; defaultOpen?: boolean;
}) {
  const [open, setOpen] = useState(defaultOpen);
  const hasChildren = !!children;
  return (
    <div className="ml-4">
      <button
        onClick={() => hasChildren && setOpen(!open)}
        className="flex items-center gap-2 py-1.5 px-2 rounded-lg hover:bg-[rgba(99,102,241,0.08)] transition-all w-full text-left group"
      >
        {hasChildren && (
          <span className={`text-[10px] text-[var(--color-text-dim)] transition-transform ${open ? 'rotate-90' : ''}`}>▶</span>
        )}
        {!hasChildren && <span className="w-3" />}
        <span className="text-sm">{icon}</span>
        <span className="text-sm text-white font-medium">{label}</span>
        {badge && (
          <span className="ml-auto text-[10px] px-2 py-0.5 rounded-full font-semibold" style={{
            background: `${badgeColor || '#7c3aed'}22`,
            color: badgeColor || '#a78bfa',
            border: `1px solid ${badgeColor || '#7c3aed'}44`
          }}>
            {badge}
          </span>
        )}
      </button>
      {open && hasChildren && (
        <div className="border-l border-[rgba(99,102,241,0.15)] ml-4 animate-fade-in">{children}</div>
      )}
    </div>
  );
}

function ElementNode({ element }: { element: SemanticElement }) {
  return (
    <TreeNode
      label={element.label || element.text || element.type}
      icon={typeIcons[element.type] || '🔷'}
      badge={element.type}
    />
  );
}

function SlideNode({ slide }: { slide: SemanticSlide }) {
  const classKey = slide.classificationType?.replace(/_/g, '').toLowerCase() || '';
  return (
    <TreeNode
      label={slide.title || `Slide ${slide.id}`}
      icon="📄"
      badge={slide.classificationType || 'unknown'}
      badgeColor={classColors[classKey]}
      defaultOpen
    >
      {slide.elements?.map((el) => <ElementNode key={el.id} element={el} />)}
      {slide.relationships?.length > 0 && (
        <TreeNode label={`Relationships (${slide.relationships.length})`} icon="🔗">
          {slide.relationships.map((rel, i) => (
            <TreeNode key={i} label={`${rel.from} → ${rel.to}`} icon="➡️" badge={rel.type || 'arrow'} />
          ))}
        </TreeNode>
      )}
    </TreeNode>
  );
}

export default function SemanticTreePage() {
  const { sessionId, semanticPresentation, setSemanticPresentation } = usePipelineStore();
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (sessionId && !semanticPresentation) {
      setLoading(true);
      api.getSemanticTree(sessionId).then(setSemanticPresentation).finally(() => setLoading(false));
    }
  }, [sessionId, semanticPresentation, setSemanticPresentation]);

  if (!sessionId) return <NoSession message="Upload a .pptx file first to view the semantic tree" />;

  return (
    <div className="animate-fade-in">
      <div className="mb-6">
        <h1 className="text-2xl font-bold gradient-text mb-1">Semantic Tree</h1>
        <p className="text-sm text-[var(--color-text-muted)]">Interactive visualization of the presentation structure</p>
      </div>

      {loading ? <Spinner /> : semanticPresentation && (
        <div className="glass-card p-5">
          <TreeNode
            label={semanticPresentation.fileName || 'Presentation'}
            icon="📊"
            badge={`${semanticPresentation.slideCount} slides`}
            badgeColor="#06b6d4"
            defaultOpen
          >
            {semanticPresentation.slides?.map((slide) => <SlideNode key={slide.id} slide={slide} />)}
          </TreeNode>
        </div>
      )}

      <div className="mt-4 flex gap-3">
        <div className="glass-card p-3 flex flex-wrap gap-2">
          {Object.entries(typeIcons).map(([type, icon]) => (
            <span key={type} className="flex items-center gap-1 text-[11px] text-[var(--color-text-dim)]">
              {icon} {type}
            </span>
          ))}
        </div>
      </div>
    </div>
  );
}

function NoSession({ message }: { message: string }) {
  return (
    <div className="flex flex-col items-center justify-center min-h-[60vh] text-center">
      <div className="text-5xl mb-4">🌳</div>
      <h2 className="text-xl font-bold text-white mb-2">No Presentation Loaded</h2>
      <p className="text-[var(--color-text-muted)]">{message}</p>
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
