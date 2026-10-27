import type { LucideIcon } from 'lucide-react'

interface KpiCardProps {
  label: string
  value: string | number
  note: string
  icon: LucideIcon
  tone: 'violet' | 'blue' | 'green' | 'amber'
  unavailable?: boolean
}

export function KpiCard({
  label,
  value,
  note,
  icon: Icon,
  tone,
  unavailable = false,
}: KpiCardProps) {
  return (
    <article className="kpi-card">
      <div className="kpi-topline">
        <span className="kpi-label">{label}</span>
        <span className={`kpi-icon kpi-${tone}`}>
          <Icon size={18} strokeWidth={1.8} />
        </span>
      </div>
      <div className={`kpi-value ${unavailable ? 'kpi-unavailable' : ''}`}>
        {value}
      </div>
      <div className="kpi-note">{note}</div>
    </article>
  )
}
