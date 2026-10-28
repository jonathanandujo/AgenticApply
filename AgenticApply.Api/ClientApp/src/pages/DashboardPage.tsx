import {
  ArrowRight,
  BriefcaseBusiness,
  CheckCheck,
  CircleCheck,
  RefreshCw,
  Sparkles,
} from 'lucide-react'
import { Link } from 'react-router-dom'
import { Charts } from '../components/Charts'
import { JobTable } from '../components/JobTable'
import { KpiCard } from '../components/KpiCard'
import { PageHeader } from '../components/PageHeader'
import { Panel } from '../components/Panel'
import { ErrorState, LoadingState } from '../components/QueryState'
import { useJobs, useStats, useTopJobs } from '../hooks/useJobs'

function getTodayLabel() {
  return new Intl.DateTimeFormat(undefined, {
    weekday: 'long',
    month: 'long',
    day: 'numeric',
  }).format(new Date()).toUpperCase()
}

export function DashboardPage() {
  const statsQuery = useStats()
  const jobsQuery = useJobs()
  const topJobsQuery = useTopJobs()

  if (statsQuery.isLoading || jobsQuery.isLoading || topJobsQuery.isLoading) {
    return <LoadingState label="Preparing your job-search dashboard…" />
  }

  if (statsQuery.isError || jobsQuery.isError || topJobsQuery.isError) {
    return (
      <ErrorState
        onRetry={() => {
          void statsQuery.refetch()
          void jobsQuery.refetch()
          void topJobsQuery.refetch()
        }}
      />
    )
  }

  const stats = statsQuery.data
  if (!stats) return <LoadingState label="Preparing your job-search dashboard…" />

  const jobs = jobsQuery.data ?? []
  const topJobs = topJobsQuery.data ?? []

  return (
    <div className="page-stack">
      <PageHeader
        eyebrow={getTodayLabel()}
        title="Your next move starts here."
        description="A clear view of your search, your matches, and what’s worth your attention."
        action={
          <button
            className="button button-secondary"
            onClick={() => {
              void statsQuery.refetch()
              void jobsQuery.refetch()
              void topJobsQuery.refetch()
            }}
          >
            <RefreshCw size={15} /> Refresh data
          </button>
        }
      />

      <section className="kpi-grid" aria-label="Job search key metrics">
        <KpiCard
          label="Jobs found"
          value={stats.totalJobs.toLocaleString()}
          note="In your database"
          icon={BriefcaseBusiness}
          tone="violet"
        />
        <KpiCard
          label="Jobs evaluated"
          value={stats.evaluatedJobs.toLocaleString()}
          note="Scored by your AI agent"
          icon={Sparkles}
          tone="blue"
        />
        <KpiCard
          label="Recommended"
          value={stats.recommendedToApply.toLocaleString()}
          note="Strong matches to explore"
          icon={CircleCheck}
          tone="green"
        />
        <KpiCard
          label="Applied jobs"
          value={stats.appliedJobs?.toLocaleString() ?? '—'}
          note={stats.appliedJobs == null ? 'Not reported by API' : 'Applications tracked'}
          icon={CheckCheck}
          tone="amber"
          unavailable={stats.appliedJobs == null}
        />
      </section>

      <Charts jobs={jobs} stats={stats} />

      <Panel
        title="Top matches"
        subtitle="The opportunities your agent thinks are worth a closer look."
        action={
          <Link className="text-link" to="/top-matches">
            View all <ArrowRight size={15} />
          </Link>
        }
        className="table-panel"
      >
        <JobTable jobs={topJobs.slice(0, 5)} />
        {topJobs.length === 0 && (
          <div className="dashboard-hint">
            <Sparkles size={15} /> Evaluations appear here after the agent scores your discovered jobs.
          </div>
        )}
      </Panel>

      <div className="dashboard-bottom-note">
        <span className="note-icon"><Sparkles size={14} /></span>
        <span>Your agent is looking for roles aligned with your experience.</span>
        <Link to="/settings">View agent details <ArrowRight size={13} /></Link>
      </div>
    </div>
  )
}
