import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type { Job, Stats } from '../types/api'
import { EmptyState } from './QueryState'
import { Panel } from './Panel'

const scoreBands = [
  { label: 'Below 60', minimum: 0, maximum: 59, color: '#596172' },
  { label: '60–69', minimum: 60, maximum: 69, color: '#707b8c' },
  { label: '70–79', minimum: 70, maximum: 79, color: '#8b94a5' },
  { label: '80–89', minimum: 80, maximum: 89, color: '#6ea7ff' },
  { label: '90–100', minimum: 90, maximum: 100, color: '#53d6a1' },
]

interface ChartsProps {
  jobs: Job[]
  stats: Stats
}

const tooltipStyle = {
  background: '#171a22',
  border: '1px solid #2a2e3a',
  borderRadius: 12,
  color: '#f4f4f7',
}

export function Charts({ jobs, stats }: ChartsProps) {
  const evaluatedJobs = jobs.filter((job) => job.evaluation)
  const distribution = scoreBands.map((band) => ({
    ...band,
    count: evaluatedJobs.filter(
      (job) =>
        job.evaluation!.score >= band.minimum &&
        job.evaluation!.score <= band.maximum,
    ).length,
  }))

  return (
    <div className="charts-grid">
      <Panel
        title="Jobs discovered"
        subtitle="Daily discovery activity"
        action={<span className="chart-period">Last 7 days</span>}
        className="chart-panel"
      >
        {stats.jobsDiscoveredByDay?.length ? (
          <div className="chart-area">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart
                data={stats.jobsDiscoveredByDay}
                margin={{ top: 12, right: 4, left: -20, bottom: 0 }}
              >
                <CartesianGrid stroke="#252934" strokeDasharray="3 5" vertical={false} />
                <XAxis
                  dataKey="date"
                  axisLine={false}
                  tickLine={false}
                  tick={{ fill: '#858b9a', fontSize: 11 }}
                />
                <YAxis
                  allowDecimals={false}
                  axisLine={false}
                  tickLine={false}
                  tick={{ fill: '#858b9a', fontSize: 11 }}
                />
                <Tooltip cursor={{ fill: 'rgba(255,255,255,.035)' }} contentStyle={tooltipStyle} />
                <Bar dataKey="count" name="Jobs found" fill="#8f78ff" radius={[5, 5, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        ) : (
          <EmptyState
            title="Discovery history is not available"
            description="The API does not expose job creation dates yet. Daily activity will appear here when that data is available."
          />
        )}
      </Panel>

      <Panel
        title="Score distribution"
        subtitle={`${evaluatedJobs.length} evaluated opportunities`}
        action={<span className="chart-legend-dot" />}
        className="chart-panel"
      >
        {evaluatedJobs.length ? (
          <div className="chart-area">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart
                data={distribution}
                margin={{ top: 12, right: 4, left: -20, bottom: 0 }}
              >
                <CartesianGrid stroke="#252934" strokeDasharray="3 5" vertical={false} />
                <XAxis
                  dataKey="label"
                  axisLine={false}
                  tickLine={false}
                  tick={{ fill: '#858b9a', fontSize: 11 }}
                />
                <YAxis
                  allowDecimals={false}
                  axisLine={false}
                  tickLine={false}
                  tick={{ fill: '#858b9a', fontSize: 11 }}
                />
                <Tooltip cursor={{ fill: 'rgba(255,255,255,.035)' }} contentStyle={tooltipStyle} />
                <Bar dataKey="count" name="Jobs" radius={[5, 5, 0, 0]}>
                  {distribution.map((band) => (
                    <Cell key={band.label} fill={band.color} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </div>
        ) : (
          <EmptyState
            title="Waiting for evaluations"
            description="Scores will be grouped into ranges as the agent evaluates jobs."
          />
        )}
      </Panel>
    </div>
  )
}
