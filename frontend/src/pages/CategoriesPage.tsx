import { useEffect, useMemo, useState } from "react";
import { categoryService } from "../services/categoryService";
import type { Category } from "../types/category";
import { hasVisibleCategoryName, sanitizeCategoryName } from "../utils/categoryName";

const wholeMonthCategoryName = "Whole Month";
const palette = [
  "#F97316",
  "#0EA5E9",
  "#EAB308",
  "#14B8A6",
  "#8B5CF6",
  "#EC4899",
  "#22C55E",
  "#3B82F6",
  "#EF4444",
  "#64748B"
];

export default function CategoriesPage() {
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [activeType, setActiveType] = useState<"expense" | "income">("expense");
  const [showArchived, setShowArchived] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);

  const [name, setName] = useState("");
  const [color, setColor] = useState(palette[0]);
  const [icon, setIcon] = useState("");

  const filtered = useMemo(
    () =>
      categories.filter(
        (category) =>
          category.type === activeType &&
          category.name !== wholeMonthCategoryName &&
          hasVisibleCategoryName(category.name)
      ),
    [categories, activeType]
  );

  const loadCategories = async () => {
    try {
      setError("");
      const data = await categoryService.getAll({
        includeArchived: showArchived
      });
      setCategories(data);
    } catch (err: any) {
      setError(err?.response?.data?.message || "Failed to load categories.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadCategories();
  }, [showArchived]);

  const resetForm = () => {
    setEditingId(null);
    setName("");
    setColor(palette[0]);
    setIcon("");
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");

    try {
      const payload = {
        name,
        type: activeType,
        color,
        icon
      };

      if (editingId) {
        await categoryService.update(editingId, payload);
      } else {
        await categoryService.create(payload);
      }

      resetForm();
      await loadCategories();
    } catch (err: any) {
      setError(err?.response?.data?.message || "Failed to save category.");
    }
  };

  const handleEdit = (category: Category) => {
    setEditingId(category.id);
    setName(category.name);
    setColor(category.color || palette[0]);
    setIcon(category.icon || "");
    setActiveType(category.type);
  };

  const handleArchiveToggle = async (category: Category) => {
    try {
      setError("");
      await categoryService.archive(category.id, !category.isArchived);
      await loadCategories();
    } catch (err: any) {
      setError(err?.response?.data?.message || "Failed to update category status.");
    }
  };

  if (loading) return <div>Loading categories...</div>;

  return (
    <div>
      <h1 className="page-title">Categories</h1>

      {error && <div className="error-banner">{error}</div>}

      <div className="cards-grid">
        <div className="card">
          <h3>Expense Categories</h3>
          <div className="big-number">
            {
              categories.filter(
                (x) =>
                  x.type === "expense" &&
                  !x.isArchived &&
                  x.name !== wholeMonthCategoryName
              ).length
            }
          </div>
        </div>
        <div className="card">
          <h3>Income Categories</h3>
          <div className="big-number">
            {categories.filter((x) => x.type === "income" && !x.isArchived).length}
          </div>
        </div>
        <div className="card">
          <h3>Archived</h3>
          <div className="big-number">{categories.filter((x) => x.isArchived).length}</div>
        </div>
      </div>

      <div className="section-grid-uneven">
        <div className="card">
          <div className="section-header">
            <h3>{editingId ? "Edit Category" : "Add Category"}</h3>
            <div className="pill-switch" role="tablist" aria-label="Category type">
              <button
                type="button"
                className={`pill-switch-btn ${activeType === "expense" ? "active" : ""}`}
                onClick={() => setActiveType("expense")}
              >
                Expense
              </button>
              <button
                type="button"
                className={`pill-switch-btn ${activeType === "income" ? "active" : ""}`}
                onClick={() => setActiveType("income")}
              >
                Income
              </button>
            </div>
          </div>

          <form className="form-grid" onSubmit={handleSubmit}>
            <div className="form-group">
              <label>Name</label>
              <input value={name} onChange={(e) => setName(e.target.value)} placeholder="e.g. Food" />
            </div>

            <div className="form-group">
              <label>Icon</label>
              <input
                value={icon}
                onChange={(e) => setIcon(e.target.value)}
                placeholder="Optional icon keyword"
              />
            </div>

            <div className="form-group form-group-full">
              <label>Color</label>
              <div className="color-grid">
                {palette.map((item) => (
                  <button
                    key={item}
                    type="button"
                    className={`color-swatch ${color === item ? "active" : ""}`}
                    style={{ backgroundColor: item }}
                    onClick={() => setColor(item)}
                    aria-label={`Select ${item}`}
                  />
                ))}
              </div>
            </div>

            <div className="form-actions">
              <button className="primary-btn" type="submit">
                {editingId ? "Update Category" : "Create Category"}
              </button>
              {editingId && (
                <button type="button" className="secondary-btn-inline" onClick={resetForm}>
                  Cancel Edit
                </button>
              )}
            </div>
          </form>
        </div>

        <div className="card">
          <div className="section-header">
            <h3>Visibility</h3>
          </div>
          <label className="checkbox-row">
            <input
              type="checkbox"
              checked={showArchived}
              onChange={(e) => setShowArchived(e.target.checked)}
            />
            <span>Show archived categories</span>
          </label>
          <p className="meta-text">
            Archived categories stay in history but are hidden from active transaction entry.
          </p>
        </div>
      </div>

      <div className="card">
        <h3>{activeType === "expense" ? "Expense Categories" : "Income Categories"}</h3>

        <div className="table-wrapper">
          <table className="app-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Color</th>
                <th>Icon</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((category) => (
                <tr key={category.id}>
                  <td>{sanitizeCategoryName(category.name)}</td>
                  <td>
                    <div className="category-color-cell">
                      <span
                        className="color-chip"
                        style={{ backgroundColor: category.color || "#94A3B8" }}
                      />
                      <span>{category.color || "-"}</span>
                    </div>
                  </td>
                  <td>{category.icon || "-"}</td>
                  <td>
                    <span className={`status-pill ${category.isArchived ? "archived" : "active"}`}>
                      {category.isArchived ? "Archived" : "Active"}
                    </span>
                  </td>
                  <td>
                    <div className="row-actions">
                      <button className="link-btn" onClick={() => handleEdit(category)}>
                        Edit
                      </button>
                      <button
                        className="link-btn danger"
                        onClick={() => handleArchiveToggle(category)}
                      >
                        {category.isArchived ? "Restore" : "Archive"}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}

              {filtered.length === 0 && (
                <tr>
                  <td colSpan={5}>No categories found for this type.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
