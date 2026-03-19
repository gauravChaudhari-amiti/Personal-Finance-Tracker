export const sanitizeCategoryName = (value: string | null | undefined) =>
  (value ?? "").replace(/[\s\p{Cf}]+/gu, " ").trim();

export const hasVisibleCategoryName = (value: string | null | undefined) =>
  sanitizeCategoryName(value).length > 0;
