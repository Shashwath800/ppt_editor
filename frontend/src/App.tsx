import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import AppLayout from './components/layout/AppLayout';
import UploadPage from './pages/UploadPage';
import OpenXmlViewerPage from './pages/OpenXmlViewerPage';
import SemanticTreePage from './pages/SemanticTreePage';
import JsonViewerPage from './pages/JsonViewerPage';
import EditPlanPage from './pages/EditPlanPage';
import DiffViewerPage from './pages/DiffViewerPage';
import RendererPage from './pages/RendererPage';
import PipelinePage from './pages/PipelinePage';
import GraphViewerPage from './pages/GraphViewerPage';
import ValidationPage from './pages/ValidationPage';
import VersionHistoryPage from './pages/VersionHistoryPage';

const router = createBrowserRouter([
  {
    element: <AppLayout />,
    children: [
      { path: '/', element: <UploadPage /> },
      { path: '/openxml-viewer', element: <OpenXmlViewerPage /> },
      { path: '/semantic-tree', element: <SemanticTreePage /> },
      { path: '/json-viewer', element: <JsonViewerPage /> },
      { path: '/edit-plan', element: <EditPlanPage /> },
      { path: '/diff-viewer', element: <DiffViewerPage /> },
      { path: '/validation', element: <ValidationPage /> },
      { path: '/renderer', element: <RendererPage /> },
      { path: '/history', element: <VersionHistoryPage /> },
      { path: '/graphs', element: <GraphViewerPage /> },
      { path: '/pipeline', element: <PipelinePage /> },
    ],
  },
]);

export default function App() {
  return <RouterProvider router={router} />;
}
