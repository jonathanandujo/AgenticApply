import { Activity, Bot, BriefcaseBusiness, Database, Sparkles } from 'lucide-react'
import { PageHeader } from '../components/PageHeader'
import { Panel } from '../components/Panel'
import { ErrorState, LoadingState } from '../components/QueryState'
import { useStats } from '../hooks/useJobs'

function formatDate(value?: string | null) {
  if (!value) return 'Not reported by API'
  const date = new Date(value)
  return Number.isNaN(date.getTime())
    ? 'Not reported by API'
    : date.toLocaleString(undefined, {
        dateStyle: 'medium',
        timeStyle: 'short',
      })
}

export function SettingsPage() {
  const query = useStats()

  return (
    <div className="page-stack">
      <PageHeader
        eyebrow="YOUR WORKSPACE"
        title="Settings"
        description="A snapshot of your agent configuration and job-search data."
      />
      {query.isLoading ? (
        <LoadingState label="Loading settings…" />
      ) : query.isError ? (
        <ErrorState onRetry={() => void query.refetch()} />
      ) : !query.data ? (
        <LoadingState label="Loading settings…" />
      ) : (
        <>
          <Panel title="Agent configuration" subtitle="Current runtime information.">
            <div className="settings-list">
              <div className="setting-row">
                <span className="setting-icon setting-purple"><Bot size={18} /></span>
                <div className="setting-copy">
                  <strong>Ollama model</strong>
                  <span>Model used by your local evaluation service.</span>
                </div>
                <code className="setting-value">
                  {import.meta.env.VITE_OLLAMA_MODEL || 'Not exposed by API'}
                </code>
              </div>
              <div className="setting-row">
                <span className="setting-icon setting-green"><Activity size={18} /></span>
                <div className="setting-copy">
                  <strong>Last execution</strong>
                  <span>Most recent worker scan reported by the API.</span>
                </div>
                <span className="setting-value">{formatDate(query.data.lastExecution)}</span>
              </div>
              <div className="setting-row">
                <span className="setting-icon setting-blue"><BriefcaseBusiness size={18} /></span>
                <div className="setting-copy">
                  <strong>Jobs in database</strong>
                  <span>Total opportunities stored in your workspace.</span>
                </div>
                <span className="setting-value">{query.data.totalJobs.toLocaleString()}</span>
              </div>
              <div className="setting-row">
                <span className="setting-icon setting-amber"><Database size={18} /></span>
                <div className="setting-copy">
                  <strong>Evaluation count</strong>
                  <span>Jobs that have received an AI score.</span>
                </div>
                <span className="setting-value">{query.data.evaluatedJobs.toLocaleString()}</span>
              </div>
            </div>
          </Panel>
          <div className="settings-info">
            <span className="info-spark"><Sparkles size={16} /></span>
            <p>
              The current API doesn’t expose worker execution time or the Ollama
              model name. Set <code>VITE_OLLAMA_MODEL</code> to show the model
              configured for your environment.
            </p>
          </div>
        </>
      )}
    </div>
  )
}
