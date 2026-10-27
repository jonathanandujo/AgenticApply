import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { lazy, Suspense } from 'react'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AppLayout } from './layouts/AppLayout'
import { LoadingState } from './components/QueryState'

const DashboardPage = lazy(() =>
  import('./pages/DashboardPage').then((module) => ({
    default: module.DashboardPage,
  })),
)
const JobDetailsPage = lazy(() =>
  import('./pages/JobDetailsPage').then((module) => ({
    default: module.JobDetailsPage,
  })),
)
const JobsPage = lazy(() =>
  import('./pages/JobsPage').then((module) => ({ default: module.JobsPage })),
)
const SettingsPage = lazy(() =>
  import('./pages/SettingsPage').then((module) => ({
    default: module.SettingsPage,
  })),
)
const TopMatchesPage = lazy(() =>
  import('./pages/TopMatchesPage').then((module) => ({
    default: module.TopMatchesPage,
  })),
)

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      refetchOnWindowFocus: false,
      retry: 1,
    },
  },
})

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route element={<AppLayout />}>
            <Route
              index
              element={
                <Suspense fallback={<LoadingState />}>
                  <Navigate to="/dashboard" replace />
                </Suspense>
              }
            />
            <Route
              path="/dashboard"
              element={<Suspense fallback={<LoadingState />}><DashboardPage /></Suspense>}
            />
            <Route
              path="/top-matches"
              element={<Suspense fallback={<LoadingState />}><TopMatchesPage /></Suspense>}
            />
            <Route
              path="/jobs"
              element={<Suspense fallback={<LoadingState />}><JobsPage /></Suspense>}
            />
            <Route
              path="/jobs/:jobId"
              element={<Suspense fallback={<LoadingState />}><JobDetailsPage /></Suspense>}
            />
            <Route
              path="/settings"
              element={<Suspense fallback={<LoadingState />}><SettingsPage /></Suspense>}
            />
            <Route path="*" element={<Navigate to="/dashboard" replace />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  )
}

export default App
