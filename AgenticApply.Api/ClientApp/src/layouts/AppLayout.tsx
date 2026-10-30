import {
  Activity,
  BriefcaseBusiness,
  ChevronDown,
  Command,
  LayoutDashboard,
  ListFilter,
  Menu,
  Settings2,
  Sparkles,
} from 'lucide-react'
import { useState } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { useStats } from '../hooks/useJobs'

const navigation = [
  { label: 'Dashboard', to: '/dashboard', icon: LayoutDashboard },
  { label: 'Top Matches', to: '/top-matches', icon: Sparkles },
  { label: 'Jobs', to: '/jobs', icon: BriefcaseBusiness },
  { label: 'Settings', to: '/settings', icon: Settings2 },
]

const FRONTEND_VERSION = '1.0.1'

function formatScanTime(value?: string | null) {
  if (!value) return 'Not reported'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return 'Not reported'
  return date.toLocaleString(undefined, {
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  })
}

function getPageTitle(pathname: string) {
  if (pathname.startsWith('/jobs/')) return 'Job details'
  return navigation.find((item) => item.to === pathname)?.label ?? 'Dashboard'
}

export function AppLayout() {
  const [mobileNavOpen, setMobileNavOpen] = useState(false)
  const location = useLocation()
  const statsQuery = useStats()
  const scanTime = formatScanTime(statsQuery.data?.lastExecution)

  return (
    <div className="app-shell">
      {mobileNavOpen && (
        <button
          className="mobile-backdrop"
          aria-label="Close navigation"
          onClick={() => setMobileNavOpen(false)}
        />
      )}
      <aside className={`sidebar ${mobileNavOpen ? 'sidebar-open' : ''}`}>
        <div className="brand">
          <span className="brand-mark">
            <Command size={19} strokeWidth={2.2} />
          </span>
          <span className="brand-name">agentic<span>apply</span></span>
          <button
            className="mobile-close"
            onClick={() => setMobileNavOpen(false)}
            aria-label="Close navigation"
          >
            ×
          </button>
        </div>

        <div className="workspace-switcher">
          <span className="workspace-avatar">J</span>
          <span className="workspace-text">
            <strong>Personal workspace</strong>
            <small>Premium</small>
          </span>
          <ChevronDown size={15} />
        </div>

        <p className="nav-label">WORKSPACE</p>
        <nav className="primary-nav" aria-label="Main navigation">
          {navigation.map(({ label, to, icon: Icon }) => (
            <NavLink
              key={to}
              to={to}
              onClick={() => setMobileNavOpen(false)}
              className={({ isActive }) =>
                `nav-link ${isActive ? 'nav-link-active' : ''}`
              }
            >
              <Icon size={18} strokeWidth={1.8} />
              <span>{label}</span>
              {label === 'Top Matches' && <span className="nav-count">AI</span>}
            </NavLink>
          ))}
        </nav>

        <div className="sidebar-bottom">
          <div className="agent-card">
            <div className="agent-card-top">
              <span className="agent-spark"><Sparkles size={15} /></span>
              <span>YOUR AI AGENT</span>
              <span className="agent-pulse" />
            </div>
            <strong>Autonomous job discovery</strong>
            <p>New opportunities, scored for your next move.</p>
            <NavLink to="/settings" className="agent-settings">
              Agent settings <span>→</span>
            </NavLink>
          </div>
          <div className="profile-row">
            <div className="profile-avatar">JA</div>
            <div className="profile-info">
              <strong>Jonathan Andujo</strong>
              <span>Principal Engineer</span>
            </div>
            <ChevronDown size={15} />
          </div>
        </div>
      </aside>

      <main className="main-shell">
        <header className="topbar">
          <div className="topbar-left">
            <button
              className="mobile-menu"
              onClick={() => setMobileNavOpen(true)}
              aria-label="Open navigation"
            >
              <Menu size={20} />
            </button>
            <div className="breadcrumbs">
              <span>Workspace</span>
              <span className="breadcrumb-separator">/</span>
              <strong>{getPageTitle(location.pathname)}</strong>
            </div>
          </div>
          <div className="topbar-right">
            <div
              className={`connection-status ${statsQuery.isError ? 'connection-offline' : ''}`}
              aria-label="Agent status based on API connectivity"
            >
              <span className="connection-dot" />
              <span className="connection-label">Agent</span>
              <strong>
                {statsQuery.isPending
                  ? 'Checking API…'
                  : statsQuery.isError
                    ? 'API unavailable'
                    : 'API reachable'}
              </strong>
            </div>
            <span className="topbar-divider" />
            <div className="last-scan">
              <Activity size={15} />
              <span>Last scan</span>
              <strong>{scanTime}</strong>
            </div>
            <button
              className="icon-button filter-button"
              aria-label="Refresh dashboard data"
              onClick={() => void statsQuery.refetch()}
            >
              <ListFilter size={17} />
            </button>
          </div>
        </header>
        <div className="content-area">
          <Outlet />
        </div>
        <footer className="app-footer">
          <span>AgenticApply <span className="footer-version">MVP</span></span>
          <span>
            Built for your next move <span className="footer-spark">✳</span>
            <span className="footer-version">UI v{FRONTEND_VERSION}</span>
          </span>
        </footer>
      </main>
    </div>
  )
}
