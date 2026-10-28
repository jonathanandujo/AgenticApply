import {
  ArrowLeft,
  ArrowUpRight,
  BriefcaseBusiness,
  MapPin,
  RefreshCw,
  Sparkles,
} from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { Panel } from '../components/Panel'
import { ErrorState, LoadingState } from '../components/QueryState'
import { ScoreBadge } from '../components/ScoreBadge'
import { StatusBadge } from '../components/StatusBadge'
import { useJob, useReevaluateJob } from '../hooks/useJobs'
import { useState } from 'react'

export function JobDetailsPage() {
  const { jobId = '' } = useParams()
  const query = useJob(jobId)
  const reevaluateMutation = useReevaluateJob(jobId)
  const [model, setModel] = useState('')

  if (query.isLoading) return <LoadingState label="Loading job details…" />
  if (query.isError) {
    return (
      <div className="page-stack">
        <Link className="back-link" to="/jobs"><ArrowLeft size={15} /> All jobs</Link>
        <ErrorState onRetry={() => void query.refetch()} />
      </div>
    )
  }

  const job = query.data
  if (!job) return <LoadingState label="Loading job details…" />

  const description = job.description.trim()

  return (
    <div className="page-stack">
      <Link className="back-link" to="/jobs"><ArrowLeft size={15} /> Back to jobs</Link>
      <div className="detail-hero">
        <div className="company-mark">{job.company.slice(0, 1).toUpperCase()}</div>
        <div className="detail-title">
          <p className="eyebrow">JOB OPPORTUNITY</p>
          <h1>{job.title}</h1>
          <div className="job-meta">
            <span><BriefcaseBusiness size={15} /> {job.company}</span>
            <span><MapPin size={15} /> {job.location}</span>
          </div>
        </div>
        <a
          className="button button-primary linkedin-button"
          href={job.url || `https://www.linkedin.com/jobs/search/?keywords=${encodeURIComponent(job.title)}`}
          target="_blank"
          rel="noreferrer"
        >
          Open LinkedIn <ArrowUpRight size={16} />
        </a>
      </div>

      <div className="details-grid">
        <Panel
          title="Role overview"
          subtitle="The details behind this opportunity."
          className="description-panel"
        >
          {description ? (
            <div className="job-description">{description}</div>
          ) : (
            <div className="description-empty">
              The job description was not available when this role was discovered.
            </div>
          )}
        </Panel>
        <div className="detail-side-stack">
          <Panel title="AI evaluation" subtitle="Your agent’s assessment.">
            <form
              className="reevaluate-form"
              onSubmit={(event) => {
                event.preventDefault()
                const modelOverride = model.trim()
                reevaluateMutation.mutate(modelOverride || undefined)
              }}
            >
              <label className="model-input-label" htmlFor="reevaluate-model">
                Model override <span>optional</span>
              </label>
              <div className="reevaluate-controls">
                <input
                  id="reevaluate-model"
                  className="model-input"
                  value={model}
                  onChange={(event) => setModel(event.target.value)}
                  maxLength={100}
                  placeholder="Use configured model"
                  disabled={reevaluateMutation.isPending}
                />
                <button
                  className="button button-secondary reevaluate-button"
                  type="submit"
                  disabled={reevaluateMutation.isPending}
                >
                  <RefreshCw
                    size={14}
                    className={reevaluateMutation.isPending ? 'spin' : ''}
                  />
                  {reevaluateMutation.isPending ? 'Scoring…' : 'Re-evaluate'}
                </button>
              </div>
              <p className="model-input-hint">
                Enter a model name such as <code>llama3.1:8b</code>, or leave
                blank to use the API’s configured model.
              </p>
            </form>
            {reevaluateMutation.isError && (
              <div className="reevaluate-feedback reevaluate-error" role="alert">
                Re-evaluation failed: {reevaluateMutation.error.message}
              </div>
            )}
            {reevaluateMutation.isSuccess && (
              <div className="reevaluate-feedback reevaluate-success" role="status">
                Evaluation updated using {reevaluateMutation.data.evaluation?.model ?? 'the configured model'}.
              </div>
            )}
            {job.evaluation ? (
              <>
                <div className="evaluation-score-row">
                  <div>
                    <span className="detail-score-label">MATCH SCORE</span>
                    <strong className="detail-score">
                      {job.evaluation.score}<small>/100</small>
                    </strong>
                  </div>
                  <ScoreBadge score={job.evaluation.score} />
                </div>
                <div className="evaluation-status">
                  <span className="detail-score-label">RECOMMENDATION</span>
                  <StatusBadge apply={job.evaluation.apply} />
                </div>
                <div className="evaluation-model">
                  <span className="detail-score-label">MODEL</span>
                  <code>{job.evaluation.model ?? 'Not recorded'}</code>
                </div>
                <div className="reason-box">
                  <span><Sparkles size={14} /> WHY THIS SCORE</span>
                  <p>{job.evaluation.reason || 'No evaluation reason was provided.'}</p>
                </div>
              </>
            ) : (
              <div className="pending-evaluation">
                <span className="pending-icon"><Sparkles size={17} /></span>
                <strong>Evaluation pending</strong>
                <p>Your agent hasn’t scored this opportunity yet.</p>
              </div>
            )}
          </Panel>
          <Panel title="Quick facts" subtitle="A few more details.">
            <div className="quick-facts">
              <div><span>Easy Apply</span><strong>{job.easyApply ? 'Available' : 'Not available'}</strong></div>
              <div><span>Apply button</span><strong>{job.applyAvailable === null ? 'Not detected' : job.applyAvailable ? 'Available' : 'Not available'}</strong></div>
              <div><span>Job ID</span><strong className="job-id">{job.jobId}</strong></div>
            </div>
          </Panel>
        </div>
      </div>
    </div>
  )
}
