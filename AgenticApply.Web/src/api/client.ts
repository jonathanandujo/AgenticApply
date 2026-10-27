import axios from 'axios'

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'https://localhost:7001',
  headers: {
    Accept: 'application/json',
  },
  timeout: 15_000,
})
