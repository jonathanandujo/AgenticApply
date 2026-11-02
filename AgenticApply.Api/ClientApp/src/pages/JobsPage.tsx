import { Search, SlidersHorizontal } from 'lucide-react'
import { useMemo, useState } from 'react'
import { JobTable } from '../components/JobTable'
import { PageHeader } from '../components/PageHeader'
import { Panel } from '../components/Panel'
import { ErrorState, LoadingState } from '../components/QueryState'
import { useJobs } from '../hooks/useJobs'

type JobFilter =
  | 'all'
  | 'recommended'
  | 'pending'
  | 'applied'
  | 'notapplied'

function isJobFilter(value: string): value is JobFilter {
  return [
    'all',
    'recommended',
    'pending',
    'applied',
    'notapplied',
  ].includes(value)
}

export function JobsPage() {
  const query = useJobs()
  const [search, setSearch] = useState('')
  const [filter, setFilter] = useState<JobFilter>('all')

  const jobs = useMemo(() => {
    const allJobs = query.data ?? []
    const needle = search.trim().toLowerCase()

    return allJobs.filter((job) => {
      const matchesSearch =
        !needle ||
        [job.title, job.company, job.location]
          .some((value) => value.toLowerCase().includes(needle))
      const matchesFilter =
        filter === 'all' ||
        (filter === 'recommended' && Boolean(job.evaluation?.apply)) ||
        (filter === 'pending' && !job.evaluation) ||
        (filter === 'applied' && job.applied) ||
        (filter === 'notapplied' && !job.applied)

      return matchesSearch && matchesFilter
    })
  }, [filter, query.data, search])

  return (
    <div className="page-stack">
      <PageHeader
        eyebrow="YOUR OPPORTUNITY PIPELINE"
        title="All jobs"
        description="Every role your agent has discovered, with evaluation status at a glance."
      />
      {query.isLoading ? (
        <LoadingState label="Loading your jobs…" />
      ) : query.isError ? (
        <ErrorState onRetry={() => void query.refetch()} />
      ) : (
        <Panel
          title="Job library"
          subtitle={`${jobs.length} of ${query.data?.length ?? 0} opportunities`}
          action={<span className="table-count">{query.data?.length ?? 0} total</span>}
          className="table-panel"
        >
          <div className="table-toolbar">
            <label className="search-field">
              <Search size={16} />
              <input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Search title, company, location…"
                aria-label="Search jobs"
              />
            </label>
            <label className="select-field">
              <SlidersHorizontal size={15} />
              <select
                value={filter}
                onChange={(event) => {
                  if (isJobFilter(event.target.value)) {
                    setFilter(event.target.value)
                  }
                }}
                aria-label="Filter jobs"
              >
                <option value="all">All jobs</option>
                <option value="recommended">Recommended</option>
                <option value="pending">Awaiting evaluation</option>
                <option value="applied">Applied</option>
                <option value="notapplied">Not Applied</option>
              </select>
            </label>
          </div>
          <JobTable jobs={jobs} showAll />
        </Panel>
      )}
    </div>
  )
}
