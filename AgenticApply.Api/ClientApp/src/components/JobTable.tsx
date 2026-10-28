import { ArrowDown, ArrowUp, ExternalLink, RefreshCw } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useReevaluateJob } from '../hooks/useJobs'
import type { Job } from '../types/api'
import { EmptyState } from './QueryState'
import { ScoreBadge } from './ScoreBadge'
import { StatusBadge } from './StatusBadge'

type SortDirection = 'asc' | 'desc'

interface JobTableProps {
  jobs: Job[]
  showAll?: boolean
}

function getScore(job: Job) {
  return job.evaluation?.score ?? -1
}

export function JobTable({ jobs, showAll = false }: JobTableProps) {
  const [sortDirection, setSortDirection] = useState<SortDirection>('desc')

  const sortedJobs = useMemo(
    () =>
      [...jobs].sort((a, b) => {
        const scoreDifference =
          sortDirection === 'desc'
            ? getScore(b) - getScore(a)
            : getScore(a) - getScore(b)
        return scoreDifference || a.title.localeCompare(b.title)
      }),
    [jobs, sortDirection],
  )

  if (jobs.length === 0) {
    return (
      <EmptyState
        title="No jobs to show yet"
        description="Once the agent discovers and evaluates opportunities, they’ll appear here."
      />
    )
  }

  return (
    <div className="table-scroll">
      <table className="jobs-table">
        <thead>
          <tr>
            <th scope="col">
              <button
                className="table-sort"
                onClick={() =>
                  setSortDirection((direction) =>
                    direction === 'desc' ? 'asc' : 'desc',
                  )
                }
                aria-label={`Sort by score ${sortDirection === 'desc' ? 'ascending' : 'descending'}`}
              >
                Score
                {sortDirection === 'desc' ? <ArrowDown size={14} /> : <ArrowUp size={14} />}
              </button>
            </th>
            <th scope="col">Apply</th>
            <th scope="col">Company</th>
            <th scope="col">Title</th>
            <th scope="col">Location</th>
            {showAll && <th scope="col">Apply option</th>}
            <th scope="col" aria-label="Open job" />
          </tr>
        </thead>
        <tbody>
          {sortedJobs.map((job) => (
            <JobTableRow key={job.jobId} job={job} showAll={showAll} />
          ))}
        </tbody>
      </table>
    </div>
  )
}

function JobTableRow({ job, showAll }: { job: Job; showAll: boolean }) {
  const reevaluateMutation = useReevaluateJob(job.jobId)

  return (
    <tr>
      <td>
        {job.evaluation ? (
          <ScoreBadge score={job.evaluation.score} />
        ) : (
          <span className="score-pending">Pending</span>
        )}
      </td>
      <td>
        {job.evaluation ? (
          <StatusBadge apply={job.evaluation.apply} />
        ) : (
          <span className="table-muted">—</span>
        )}
      </td>
      <td className="company-cell">{job.company}</td>
      <td>
        <Link
          className="job-title-link"
          to={`/jobs/${encodeURIComponent(job.jobId)}`}
        >
          {job.title}
        </Link>
      </td>
      <td className="location-cell">{job.location}</td>
      {showAll && (
        <td>
          <span className="table-muted">
            {job.applyAvailable === null
              ? 'Unknown'
              : job.easyApply
              ? 'Easy Apply'
              : job.applyAvailable
                ? 'External Apply'
                : 'No'}
          </span>
        </td>
      )}
      <td>
        <div className="row-actions">
          <button
            className="row-action"
            type="button"
            aria-label={`Re-evaluate ${job.title} with configured model`}
            title={
              reevaluateMutation.isError
                ? `Re-evaluation failed: ${reevaluateMutation.error.message}`
                : 'Re-evaluate with configured model'
            }
            disabled={reevaluateMutation.isPending}
            onClick={() => reevaluateMutation.mutate(undefined)}
          >
            <RefreshCw
              size={14}
              className={reevaluateMutation.isPending ? 'spin' : ''}
            />
          </button>
          <Link
            className="row-action"
            to={`/jobs/${encodeURIComponent(job.jobId)}`}
            aria-label={`View ${job.title}`}
          >
            <ExternalLink size={15} />
          </Link>
        </div>
      </td>
    </tr>
  )
}
