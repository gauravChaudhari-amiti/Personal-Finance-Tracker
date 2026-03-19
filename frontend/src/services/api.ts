import axios from "axios";

const fallbackApiBaseUrl = `${window.location.protocol}//${window.location.hostname}:5000/api`;

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || fallbackApiBaseUrl
});

api.interceptors.request.use((config) => {
  const raw = localStorage.getItem("pft_user");

  if (raw) {
    const user = JSON.parse(raw);
    config.headers.Authorization = `Bearer ${user.token}`;
  }

  return config;
});
