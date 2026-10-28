import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  getJob,
  getJobs,
  getLinkedInSearchSettings,
  getStats,
  getTopJobs,
  reevaluateJob,
  updateLinkedInSearchUrl,
} from '../api/jobs'

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

export function useLinkedInSearchSettings() {
  return useQuery({
    queryKey: ['settings', 'linkedin-search'],
    queryFn: ({ signal }) => getLinkedInSearchSettings(signal),
  })
}

export function useUpdateLinkedInSearchUrl() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: updateLinkedInSearchUrl,
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ['settings', 'linkedin-search'],
      })
    },
  })
}

export function useReevaluateJob(jobId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (model?: string) => reevaluateJob(jobId, model),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['jobs'] }),
        queryClient.invalidateQueries({ queryKey: ['stats'] }),
      ])
    },
  })
}
