import { useQuery } from '@tanstack/react-query'
import { getJob, getJobs, getStats, getTopJobs } from '../api/jobs'

export function useJobs() {
  return useQuery({
    queryKey: ['jobs'],
    queryFn: ({ signal }) => getJobs(signal),
  })
}

export function useTopJobs() {
  return useQuery({
    queryKey: ['jobs', 'top'],
    queryFn: ({ signal }) => getTopJobs(signal),
  })
}

export function useJob(jobId: string) {
  return useQuery({
    queryKey: ['jobs', jobId],
    queryFn: ({ signal }) => getJob(jobId, signal),
    enabled: Boolean(jobId),
  })
}

export function useStats() {
  return useQuery({
    queryKey: ['stats'],
    queryFn: ({ signal }) => getStats(signal),
  })
}
