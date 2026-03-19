import { api } from "./api";
import type { ReportFilters, ReportResponse } from "../types/report";

export const reportService = {
  async getSummary(filters: ReportFilters) {
    const response = await api.get<ReportResponse>("/reports/summary", {
      params: filters
    });
    return response.data;
  },

  async exportCsv(filters: ReportFilters) {
    const response = await api.get<Blob>("/reports/export/csv", {
      params: filters,
      responseType: "blob"
    });
    return response.data;
  }
};
