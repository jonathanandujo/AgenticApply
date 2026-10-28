export function StatusBadge({ apply }: { apply: boolean }) {
  return (
    <span className={`status-badge ${apply ? 'status-positive' : 'status-neutral'}`}>
      <span className="status-dot" />
      {apply ? 'Recommended' : 'Not now'}
    </span>
  )
}
