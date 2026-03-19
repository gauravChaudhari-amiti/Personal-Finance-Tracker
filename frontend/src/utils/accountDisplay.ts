export const formatAccountDisplayName = (name: string, type?: string | null) => {
  const trimmedName = name.trim();
  const trimmedType = type?.trim();

  if (!trimmedName) {
    return trimmedType ? `(${trimmedType})` : "";
  }

  return trimmedType ? `${trimmedName} (${trimmedType})` : trimmedName;
};
