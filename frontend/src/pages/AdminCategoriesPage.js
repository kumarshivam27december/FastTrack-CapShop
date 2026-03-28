import { useCallback, useEffect, useState } from 'react';
import { adminApi } from '../api/adminApi';
import { useAuth } from '../context/AuthContext';
import LoadingSpinner from '../components/LoadingSpinner';

const INITIAL_CATEGORY_FORM = {
  id: null,
  name: '',
  description: '',
  isActive: true
};

export default function AdminCategoriesPage() {
  const { token } = useAuth();
  const [categories, setCategories] = useState([]);
  const [categoryForm, setCategoryForm] = useState(INITIAL_CATEGORY_FORM);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const loadCategories = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const data = await adminApi.getCategories();
      setCategories(Array.isArray(data) ? data : []);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadCategories();
  }, [loadCategories]);

  function populateCategory(category) {
    setCategoryForm({
      id: category.id,
      name: category.name,
      description: category.description,
      isActive: true
    });
  }

  function clearCategoryForm() {
    setCategoryForm(INITIAL_CATEGORY_FORM);
  }

  async function handleCategorySubmit(event) {
    event.preventDefault();

    const payload = {
      name: categoryForm.name,
      description: categoryForm.description,
      isActive: categoryForm.isActive
    };

    try {
      if (categoryForm.id) {
        await adminApi.updateCategory(token, categoryForm.id, payload);
      } else {
        await adminApi.createCategory(token, payload);
      }

      clearCategoryForm();
      await loadCategories();
    } catch (err) {
      alert(err.message);
    }
  }

  async function handleDeleteCategory(id) {
    if (!window.confirm('Delete this category?')) {
      return;
    }

    try {
      await adminApi.deleteCategory(token, id);
      await loadCategories();
    } catch (err) {
      alert(err.message);
    }
  }

  if (loading) {
    return <LoadingSpinner label="Loading categories..." />;
  }

  return (
    <div>
      <h1>Category Management</h1>
      {error && <p className="message error">{error}</p>}

      <section className="card section">
        <div className="section-head">
          <h2>{categoryForm.id ? 'Edit Category' : 'Add New Category'}</h2>
          {categoryForm.id ? (
            <button type="button" className="btn btn-outline" onClick={clearCategoryForm}>Cancel Edit</button>
          ) : null}
        </div>

        <form className="product-form" onSubmit={handleCategorySubmit}>
          <input
            placeholder="Category Name"
            value={categoryForm.name}
            onChange={(e) => setCategoryForm((prev) => ({ ...prev, name: e.target.value }))}
            required
          />
          <input
            placeholder="Description"
            value={categoryForm.description}
            onChange={(e) => setCategoryForm((prev) => ({ ...prev, description: e.target.value }))}
            required
          />
          <label className="inline-actions">
            <input
              type="checkbox"
              checked={categoryForm.isActive}
              onChange={(e) => setCategoryForm((prev) => ({ ...prev, isActive: e.target.checked }))}
            />
            Active
          </label>
          <button type="submit" className="btn btn-solid">
            {categoryForm.id ? 'Update Category' : 'Add Category'}
          </button>
        </form>
      </section>

      <section className="card section">
        <h2>Existing Categories</h2>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Description</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {categories.map((category) => (
                <tr key={category.id}>
                  <td>{category.name}</td>
                  <td>{category.description}</td>
                  <td>
                    <div className="inline-actions">
                      <button type="button" className="btn btn-outline" onClick={() => populateCategory(category)}>
                        Edit
                      </button>
                      <button
                        type="button"
                        className="btn btn-outline"
                        onClick={() => handleDeleteCategory(category.id)}
                      >
                        Delete
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}
