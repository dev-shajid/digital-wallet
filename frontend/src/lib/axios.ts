import axios from 'axios';

export const api = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8000/api/v1', // Replace with your API base URL
  headers: {
    'Content-Type': 'application/json',
  },
});

// Optional: Add request/response interceptors here (e.g., attaching auth tokens)
