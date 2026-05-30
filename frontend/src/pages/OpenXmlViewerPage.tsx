import { useState, useEffect } from 'react';
import { usePipelineStore } from '../store/pipelineStore';
import { api } from '../services/api';
import type { OpenXmlSlideInfo } from '../types';

function formatXml(xml: string) {
  let formatted = '';
  const reg = /(>)(<)(\/*)/g;
  let pad = 0;
  xml = xml.replace(reg, '$1\r\n$2$3');
  
  xml.split('\r\n').forEach((node) => {
    let indent = 0;
    if (node.match(/.+<\/\w[^>]*>$/)) { indent = 0; }
    else if (node.match(/^<\/\w/)) { if (pad !== 0) { pad -= 1; } }
    else if (node.match(/^<\w[^>]*[^\/]>.*$/)) { indent = 1; }
    else { indent = 0; }
    
    formatted += '  '.repeat(pad) + node + '\r\n';
    pad += indent;
  });
  
  return formatted;
}

function XmlHighlighter({ xml }: { xml: string }) {
  const prettyXml = formatXml(xml);
  const highlighted = prettyXml
    .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
    .replace(/(&lt;\/?[\w:]+)/g, '<span style="color:#7c3aed">$1</span>')
    .replace(/([\w:]+)=/g, '<span style="color:#06b6d4">$1</span>=')
    .replace(/"([^"]*)"/g, '"<span style="color:#10b981">$1</span>"');
  return (
    <pre
      className="text-xs leading-relaxed overflow-auto max-h-[600px] p-4 bg-[rgba(0,0,0,0.3)] rounded-lg font-[var(--font-mono)] whitespace-pre-wrap break-all"
      dangerouslySetInnerHTML={{ __html: highlighted }}
    />
  );
}

export default function OpenXmlViewerPage() {
  const { sessionId, openXmlInfo, setOpenXmlInfo } = usePipelineStore();
  const [selectedSlide, setSelectedSlide] = useState<number>(0);
  const [slideDetail, setSlideDetail] = useState<OpenXmlSlideInfo | null>(null);
  const [loading, setLoading] = useState(false);
  const [activeTab, setActiveTab] = useState<'xml' | 'shapes' | 'relationships'>('xml');

  useEffect(() => {
    if (sessionId && !openXmlInfo) {
      setLoading(true);
      api.getOpenXmlInfo(sessionId).then(setOpenXmlInfo).finally(() => setLoading(false));
    }
  }, [sessionId, openXmlInfo, setOpenXmlInfo]);

  useEffect(() => {
    if (openXmlInfo?.slides?.[selectedSlide]) {
      setSlideDetail(openXmlInfo.slides[selectedSlide]);
    }
  }, [selectedSlide, openXmlInfo]);

  if (!sessionId) return <NoSession />;

  return (
    <div className="animate-fade-in">
      <div className="mb-6">
        <h1 className="text-2xl font-bold gradient-text mb-1">OpenXML Viewer</h1>
        <p className="text-sm text-[var(--color-text-muted)]">Inspect raw OpenXML structure for debugging and transparency</p>
      </div>

      {loading ? <Spinner /> : (
        <div className="grid grid-cols-[280px_1fr] gap-4 min-h-[600px]">
          {/* Slide Tree */}
          <div className="glass-card p-4">
            <h3 className="text-sm font-semibold text-white mb-3">📊 Presentation</h3>
            <div className="space-y-1">
              {openXmlInfo?.slides.map((slide, i) => (
                <button
                  key={i}
                  onClick={() => setSelectedSlide(i)}
                  className={`w-full text-left px-3 py-2 rounded-lg text-sm transition-all ${
                    selectedSlide === i
                      ? 'bg-gradient-to-r from-[rgba(124,58,237,0.2)] to-[rgba(59,130,246,0.15)] text-white'
                      : 'text-[var(--color-text-muted)] hover:bg-[rgba(99,102,241,0.08)]'
                  }`}
                >
                  📄 Slide {i + 1}
                  <span className="text-[10px] ml-2 text-[var(--color-text-dim)]">
                    {slide.shapes?.length || 0} shapes
                  </span>
                </button>
              ))}
            </div>
          </div>

          {/* Detail Panel */}
          <div className="glass-card p-5">
            <div className="flex gap-2 mb-4">
              {(['xml', 'shapes', 'relationships'] as const).map((tab) => (
                <button
                  key={tab}
                  onClick={() => setActiveTab(tab)}
                  className={`px-4 py-1.5 rounded-lg text-xs font-semibold uppercase tracking-wide transition-all ${
                    activeTab === tab
                      ? 'bg-[rgba(124,58,237,0.2)] text-[var(--color-primary-light)]'
                      : 'text-[var(--color-text-dim)] hover:text-white'
                  }`}
                >
                  {tab === 'xml' ? '📝 Raw XML' : tab === 'shapes' ? '🔷 Shapes' : '🔗 Relationships'}
                </button>
              ))}
            </div>

            {activeTab === 'xml' && slideDetail?.rawXml && (
              <XmlHighlighter xml={slideDetail.rawXml} />
            )}

            {activeTab === 'shapes' && (
              <div className="space-y-3 max-h-[500px] overflow-auto">
                {slideDetail?.shapes?.map((shape, i) => (
                  <details key={i} className="glass-card p-3 cursor-pointer">
                    <summary className="flex items-center gap-2 text-sm font-medium">
                      <span className="badge badge-primary text-[10px]">{shape.type}</span>
                      <span className="text-white">{shape.name || `Shape ${shape.shapeId}`}</span>
                      {shape.text && <span className="text-[var(--color-text-dim)] text-xs truncate max-w-[200px]">"{shape.text}"</span>}
                    </summary>
                    {shape.rawXml && (
                      <div className="mt-3">
                        <XmlHighlighter xml={shape.rawXml} />
                      </div>
                    )}
                  </details>
                ))}
                {(!slideDetail?.shapes || slideDetail.shapes.length === 0) && (
                  <p className="text-[var(--color-text-dim)] text-sm text-center py-8">No shapes found</p>
                )}
              </div>
            )}

            {activeTab === 'relationships' && (
              <div className="overflow-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-[rgba(99,102,241,0.15)]">
                      <th className="text-left py-2 px-3 text-[var(--color-text-dim)] font-medium text-xs">ID</th>
                      <th className="text-left py-2 px-3 text-[var(--color-text-dim)] font-medium text-xs">Type</th>
                      <th className="text-left py-2 px-3 text-[var(--color-text-dim)] font-medium text-xs">Target</th>
                    </tr>
                  </thead>
                  <tbody>
                    {slideDetail?.relationships?.map((rel, i) => (
                      <tr key={i} className="border-b border-[rgba(99,102,241,0.08)] hover:bg-[rgba(99,102,241,0.05)]">
                        <td className="py-2 px-3 font-mono text-[var(--color-accent)] text-xs">{rel.id}</td>
                        <td className="py-2 px-3 text-[var(--color-text-muted)] text-xs">{rel.type?.split('/').pop()}</td>
                        <td className="py-2 px-3 text-[var(--color-text-muted)] text-xs">{rel.target}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
                {(!slideDetail?.relationships || slideDetail.relationships.length === 0) && (
                  <p className="text-[var(--color-text-dim)] text-sm text-center py-8">No relationships found</p>
                )}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

function NoSession() {
  return (
    <div className="flex flex-col items-center justify-center min-h-[60vh] text-center">
      <div className="text-5xl mb-4">📄</div>
      <h2 className="text-xl font-bold text-white mb-2">No Presentation Loaded</h2>
      <p className="text-[var(--color-text-muted)]">Upload a .pptx file first to view OpenXML data</p>
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
