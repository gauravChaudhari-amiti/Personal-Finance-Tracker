import axios from "axios";

const fallbackApiBaseUrl = `${window.location.protocol}//${window.location.hostname}:5000/api`;

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || fallbackApiBaseUrl,
  withCredentials: true
});
