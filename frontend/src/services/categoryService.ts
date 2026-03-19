import { api } from "./api";
import type {
  Category,
  CreateCategoryRequest,
  UpdateCategoryRequest
} from "../types/category";

export const categoryService = {
  async getAll(filters?: { type?: string; includeArchived?: boolean }) {
    const response = await api.get<Category[]>("/categories", {
      params: filters
    });
    return response.data;
  },

  async create(payload: CreateCategoryRequest) {
    const response = await api.post<Category>("/categories", payload);
    return response.data;
  },

  async update(id: string, payload: UpdateCategoryRequest) {
    const response = await api.put<Category>(`/categories/${id}`, payload);
    return response.data;
  },

  async archive(id: string, isArchived: boolean) {
    const response = await api.post<Category>(`/categories/${id}/archive`, null, {
      params: { isArchived }
    });
    return response.data;
  }
};
