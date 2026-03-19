export type Category = {
  id: string;
  userId: string;
  name: string;
  type: "income" | "expense";
  color?: string | null;
  icon?: string | null;
  isArchived: boolean;
};

export type CreateCategoryRequest = {
  name: string;
  type: "income" | "expense";
  color?: string;
  icon?: string;
};

export type UpdateCategoryRequest = CreateCategoryRequest;
