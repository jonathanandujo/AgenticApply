import { Sparkles } from 'lucide-react'
import { JobTable } from '../components/JobTable'
import { PageHeader } from '../components/PageHeader'
import { Panel } from '../components/Panel'
import { ErrorState, LoadingState } from '../components/QueryState'
import { useTopJobs } from '../hooks/useJobs'

export function TopMatchesPage() {
  const query = useTopJobs()

  return (
    <div className="page-stack">
      <PageHeader
        eyebrow="CURATED FOR YOU"
        title="Top matches"
        description="Your highest-scoring roles, ranked by how well they fit your profile."
        action={
          <span className="insight-pill">
            <Sparkles size={14} /> AI ranked
          </span>
        }
      />
      {query.isLoading ? (
        <LoadingState label="Finding your strongest matches…" />
      ) : query.isError ? (
        <ErrorState onRetry={() => void query.refetch()} />
      ) : (
        <Panel
          title="Best-fit opportunities"
          subtitle={`${query.data?.length ?? 0} evaluated matches · Sorted by score`}
          className="table-panel"
        >
          <JobTable jobs={query.data ?? []} />
        </Panel>
      )}
    </div>
  )
}
