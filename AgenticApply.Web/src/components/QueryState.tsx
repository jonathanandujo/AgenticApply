import { AlertCircle, LoaderCircle } from 'lucide-react'

export function LoadingState({ label = 'Loading data…' }: { label?: string }) {
  return (
    <div className="query-state" role="status">
      <LoaderCircle className="spin" size={20} />
      <span>{label}</span>
    </div>
  )
}

export function ErrorState({ onRetry }: { onRetry?: () => void }) {
  return (
    <div className="query-state query-error" role="alert">
      <AlertCircle size={20} />
      <div>
        <strong>Couldn’t load AgenticApply data</strong>
        <p>Check that the API is running and accessible from this browser.</p>
      </div>
      {onRetry && (
        <button className="button button-secondary" onClick={onRetry}>
          Retry
        </button>
      )}
    </div>
  )
}

export function EmptyState({
  title,
  description,
}: {
  title: string
  description: string
}) {
  return (
    <div className="empty-state">
      <span className="empty-mark">◎</span>
      <strong>{title}</strong>
      <p>{description}</p>
    </div>
  )
}
