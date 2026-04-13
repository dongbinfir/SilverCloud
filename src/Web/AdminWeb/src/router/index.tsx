import { Navigate, createBrowserRouter } from 'react-router-dom'
import LoginPage from '@pages/login'
import DashboardPage from '@pages/dashboard'
import ProfilesPage from '@pages/profiles'
import AuthorizationsPage from '@pages/authorizations'
import AdminLayout from '@layouts/AdminLayout'
import { useAuthStore } from '@store/authStore'

function ProtectedRoute({ children }: { children: React.ReactElement }) {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated)
  return isAuthenticated ? children : <Navigate to="/login" replace />
}

const router = createBrowserRouter([
  {
    path: '/login',
    element: <LoginPage />,
  },
  {
    path: '/',
    element: (
      <ProtectedRoute>
        <AdminLayout />
      </ProtectedRoute>
    ),
    children: [
      {
        path: 'dashboard',
        element: <DashboardPage />,
      },
      {
        path: 'profiles',
        element: <ProfilesPage />,
      },
      {
        path: 'authorizations',
        element: <AuthorizationsPage />,
      },
      {
        path: '*',
        element: <Navigate to="/dashboard" replace />,
      },
    ],
  },
  {
    path: '*',
    element: <Navigate to="/login" replace />,
  },
])

export default router