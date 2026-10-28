import type { Job, Stats } from '../types/api'
import { apiClient } from './client'

export async function getJobs(signal?: AbortSignal): Promise<Job[]> {
  const { data } = await apiClient.get<Job[]>('/jobs', { signal })
  return data
}

export async function getTopJobs(signal?: AbortSignal): Promise<Job[]> {
  const { data } = await apiClient.get<Job[]>('/jobs/top', { signal })
  return data
}

export async function getJob(jobId: string, signal?: AbortSignal): Promise<Job> {
  const { data } = await apiClient.get<Job>(
    `/jobs/${encodeURIComponent(jobId)}`,
    { signal },
  )
  return data
}

export async function reevaluateJob(
  jobId: string,
  model?: string,
): Promise<Job> {
  const { data } = await apiClient.post<Job>(
    `/jobs/${encodeURIComponent(jobId)}/reevaluate`,
    model ? { model } : {},
    { timeout: 180_000 },
  )
  return data
}

export async function getStats(signal?: AbortSignal): Promise<Stats> {
  const { data } = await apiClient.get<Stats>('/stats', { signal })
  return data
}
