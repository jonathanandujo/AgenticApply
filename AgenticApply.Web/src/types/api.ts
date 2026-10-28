export interface Evaluation {
  score: number
  apply: boolean
  reason: string
  model: string | null
}

export interface Job {
  jobId: string
  title: string
  company: string
  location: string
  description: string
  easyApply: boolean
  url: string
  evaluation: Evaluation | null
  createdAt?: string
}

export interface DailyDiscovery {
  date: string
  count: number
}

export interface Stats {
  totalJobs: number
  evaluatedJobs: number
  recommendedToApply: number
  averageScore: number | null
  appliedJobs?: number
  lastExecution?: string | null
  jobsDiscoveredByDay?: DailyDiscovery[]
}
