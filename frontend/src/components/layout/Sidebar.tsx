import { NavLink } from 'react-router-dom';
import { usePipelineStore } from '../../store/pipelineStore';

const navItems = [
  { path: '/', label: 'Upload', icon: '📤' },
  { path: '/openxml-viewer', label: 'OpenXML Viewer', icon: '📄' },
  { path: '/semantic-tree', label: 'Semantic Tree', icon: '🌳' },
  { path: '/json-viewer', label: 'JSON Viewer', icon: '📋' },
  { path: '/edit-plan', label: 'Edit Plan', icon: '✏️' },
  { path: '/diff-viewer', label: 'Diff Viewer', icon: '🔀' },
  { path: '/validation', label: 'Validation', icon: '✅' },
  { path: '/renderer', label: 'Renderer', icon: '🔧' },
  { path: '/history', label: 'Version History', icon: '🕒' },
  { path: '/graphs', label: 'Semantic Graphs', icon: '🕸️' },
  { path: '/pipeline', label: 'Pipeline', icon: '🔄' },
];

export default function Sidebar() {
  const { sessionId, pipelineState, isSidebarOpen, toggleSidebar } = usePipelineStore();

  return (
    <aside 
      className={`fixed left-0 top-0 bottom-0 flex flex-col border-r border-[rgba(99,102,241,0.15)] bg-[rgba(10,15,30,0.95)] backdrop-blur-xl z-50 transition-all duration-300 ${isSidebarOpen ? 'w-[260px]' : 'w-[72px]'}`}
    >
      <div className="p-5 border-b border-[rgba(99,102,241,0.15)] flex items-center justify-between">
        {isSidebarOpen && (
          <div className="animate-fade-in overflow-hidden whitespace-nowrap">
            <h1 className="text-lg font-bold gradient-text tracking-tight">⚡ PPT Editor</h1>
          </div>
        )}
        <button 
          onClick={toggleSidebar}
          className="text-[var(--color-text-muted)] hover:text-white transition-colors flex-shrink-0"
          title={isSidebarOpen ? "Collapse sidebar" : "Expand sidebar"}
        >
          {isSidebarOpen ? '◀' : '▶'}
        </button>
      </div>

      <nav className="flex-1 py-3 overflow-y-auto">
        {navItems.map((item) => (
          <NavLink
            key={item.path}
            to={item.path}
            title={!isSidebarOpen ? item.label : undefined}
            className={({ isActive }) =>
              `flex items-center px-5 py-2.5 mx-2 rounded-lg text-[13px] font-medium transition-all duration-200 ${
                isSidebarOpen ? 'gap-3' : 'justify-center'
              } ${
                isActive
                  ? 'bg-gradient-to-r from-[rgba(124,58,237,0.2)] to-[rgba(59,130,246,0.15)] text-white border-l-2 border-[var(--color-primary)]'
                  : 'text-[var(--color-text-muted)] hover:text-white hover:bg-[rgba(99,102,241,0.08)] border-l-2 border-transparent'
              }`
            }
          >
            <span className="text-xl">{item.icon}</span>
            {isSidebarOpen && <span className="animate-fade-in whitespace-nowrap overflow-hidden">{item.label}</span>}
          </NavLink>
        ))}
      </nav>

      {sessionId && isSidebarOpen && (
        <div className="p-4 border-t border-[rgba(99,102,241,0.15)] animate-fade-in">
          <div className="glass-card p-3">
            <p className="text-[10px] text-[var(--color-text-dim)] uppercase tracking-wider mb-1">Session</p>
            <p className="text-[11px] text-[var(--color-accent)] font-mono truncate">{sessionId}</p>
            {pipelineState && (
              <p className="text-[10px] text-[var(--color-text-dim)] mt-1.5 truncate">
                📁 {pipelineState.fileName}
              </p>
            )}
          </div>
        </div>
      )}
      
      {sessionId && !isSidebarOpen && (
        <div className="p-4 border-t border-[rgba(99,102,241,0.15)] flex justify-center text-xl text-[var(--color-text-dim)]" title={pipelineState?.fileName || sessionId}>
          📁
        </div>
      )}
    </aside>
  );
}
