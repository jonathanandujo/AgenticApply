import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  getJob,
  getJobs,
  getLinkedInSearchSettings,
  getOllamaSettings,
  getStats,
  getTopJobs,
  markJobApplied,
  requestAgentScan,
  reevaluateJob,
  updateLinkedInSearchUrl,
  updateOllamaModel,
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

export function useOllamaSettings() {
  return useQuery({
    queryKey: ['settings', 'ollama'],
    queryFn: ({ signal }) => getOllamaSettings(signal),
  })
}

export function useUpdateOllamaModel() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: updateOllamaModel,
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ['settings', 'ollama'],
      })
    },
  })
}

export function useRequestAgentScan() {
  return useMutation({
    mutationFn: requestAgentScan,
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

export function useMarkJobApplied(jobId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => markJobApplied(jobId),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['jobs'] }),
        queryClient.invalidateQueries({ queryKey: ['stats'] }),
      ])
    },
  })
}
