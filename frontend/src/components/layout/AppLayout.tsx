import { Outlet } from 'react-router-dom';
import Sidebar from './Sidebar';
import { usePipelineStore } from '../../store/pipelineStore';

export default function AppLayout() {
  const { isSidebarOpen } = usePipelineStore();
  
  return (
    <div className="min-h-screen bg-[var(--color-bg-primary)] transition-all duration-300">
      <Sidebar />
      <main className={`min-h-screen transition-all duration-300 flex justify-center ${isSidebarOpen ? 'ml-[260px]' : 'ml-[72px]'}`}>
        <div className="p-6 w-full max-w-[1200px] animate-fade-in flex flex-col">
          <Outlet />
        </div>
      </main>
    </div>
  );
}
