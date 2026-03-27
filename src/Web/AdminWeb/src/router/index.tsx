import { Navigate, createBrowserRouter } from 'react-router-dom'
import LoginPage from '../pages/login'

const router = createBrowserRouter([
  {
    path: '/',
    element: <Navigate to="/login" replace />,
  },
  {
    path: '/login',
    element: <LoginPage />,
  },
  {
    path: '*',
    element: <Navigate to="/login" replace />,
  },
])

export default router