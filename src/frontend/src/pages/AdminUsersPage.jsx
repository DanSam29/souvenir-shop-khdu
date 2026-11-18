import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { adminUsersAPI } from '../services/api';

function AdminUsersPage() {
  const { t } = useTranslation();
  const [users, setUsers] = useState([]);
  const [pagination, setPagination] = useState({ pageNumber: 1, totalPages: 1 });
  const [loading, setLoading] = useState(true);
  const [updating, setUpdating] = useState(false);
  useEffect(() => {
    loadUsers(1);
  }, []);
  const loadUsers = async (page = 1) => {
    try {
      setLoading(true);
      const res = await adminUsersAPI.getAll({ pageNumber: page, pageSize: 10 });
      // Повернення об'єкту з Items та метаданими
      if (res.data && res.data.items) {
        setUsers(res.data.items);
        setPagination({
          pageNumber: res.data.pageNumber,
          totalPages: res.data.totalPages
        });
      } else {
        setUsers(res.data || []);
      }
    } catch (err) {
      console.error(err);
      setUsers([]);
    } finally {
      setLoading(false);
    }
  };
  const handleRoleChange = async (id, newRole) => {
    try {
      setUpdating(true);
      await adminUsersAPI.updateRole(id, { role: newRole });
      alert(t('admin.users.role_updated'));
      loadUsers();
    } catch (err) {
      alert(t('admin.users.role_update_error'));
    } finally {
      setUpdating(false);
    }
  };
  if (loading) return <div className="loading">{t('common.loading')}</div>;
  return (
    <div className="admin-users" style={{ padding: 20, maxWidth: 1400, margin: '0 auto' }}>
      <div style={{ marginBottom: 20 }}>
        <Link to="/admin" className="back-link">
          {t('admin.common.back_to_dashboard')}
        </Link>
        <h1>{t('admin.users.title')}</h1>
      </div>
      <div style={{ background: '#fff', borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.05)', overflowX: 'auto' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead style={{ background: '#f8f9fa' }}>
            <tr>
              <th style={{ padding: '16px', textAlign: 'left', borderBottom: '1px solid #eee' }}>{t('admin.users.col_id')}</th>
              <th style={{ padding: '16px', textAlign: 'left', borderBottom: '1px solid #eee' }}>{t('admin.users.col_first_name')}</th>
              <th style={{ padding: '16px', textAlign: 'left', borderBottom: '1px solid #eee' }}>{t('admin.users.col_last_name')}</th>
              <th style={{ padding: '16px', textAlign: 'left', borderBottom: '1px solid #eee' }}>{t('admin.users.col_email')}</th>
              <th style={{ padding: '16px', textAlign: 'left', borderBottom: '1px solid #eee' }}>{t('admin.users.col_role')}</th>
              <th style={{ padding: '16px', textAlign: 'left', borderBottom: '1px solid #eee' }}>{t('admin.users.col_student_status')}</th>
              <th style={{ padding: '16px', textAlign: 'left', borderBottom: '1px solid #eee' }}>{t('admin.users.col_created')}</th>
              <th style={{ padding: '16px', textAlign: 'left', borderBottom: '1px solid #eee' }}>{t('admin.common.actions')}</th>
            </tr>
          </thead>
          <tbody>
            {users.map(u => (
              <tr key={u.userId} style={{ borderBottom: '1px solid #f0f0f0' }}>
                <td style={{ padding: '16px' }}>{u.userId}</td>
                <td style={{ padding: '16px' }}>{u.firstName}</td>
                <td style={{ padding: '16px' }}>{u.lastName}</td>
                <td style={{ padding: '16px' }}>{u.email}</td>
                <td style={{ padding: '16px' }}>
                  <span style={{ 
                    padding: '4px 12px', 
                    borderRadius: 20, 
                    fontSize: '0.85rem',
                    background: u.role === 'Administrator' ? '#fff3cd' : (u.role === 'Manager' ? '#d1ecf1' : '#d4edda'),
                    color: u.role === 'Administrator' ? '#856404' : (u.role === 'Manager' ? '#0c5460' : '#155724')
                  }}>
                    {u.role}
                  </span>
                </td>
                <td style={{ padding: '16px' }}>{u.studentStatus}</td>
                <td style={{ padding: '16px' }}>{new Date(u.createdAt).toLocaleDateString()}</td>
                <td style={{ padding: '16px' }}>
                  <select 
                    value={u.role} 
                    onChange={(e) => handleRoleChange(u.userId, e.target.value)}
                    disabled={updating}
                    style={{ padding: '6px 12px', borderRadius: 6, border: '1px solid #ddd' }}
                  >
                    <option value="Customer">Customer</option>
                    <option value="Manager">Manager</option>
                    <option value="Administrator">Administrator</option>
                  </select>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {users.length === 0 && (
          <div style={{ padding: 40, textAlign: 'center', color: '#666' }}>
            {t('admin.users.empty')}
          </div>
        )}
      </div>
      {pagination.totalPages > 1 && (
        <div style={{ marginTop: 20, display: 'flex', justifyContent: 'center', gap: 10 }}>
          <button 
            onClick={() => loadUsers(pagination.pageNumber - 1)} 
            disabled={pagination.pageNumber === 1 || loading}
            style={{ padding: '8px 16px', borderRadius: 6, border: '1px solid #ddd', background: '#fff', cursor: pagination.pageNumber === 1 ? 'not-allowed' : 'pointer' }}
          >
            ← {t('common.prev')}
          </button>
          <span style={{ alignSelf: 'center' }}>
            {t('admin.common.page_of', { current: pagination.pageNumber, total: pagination.totalPages })}
          </span>
          <button 
            onClick={() => loadUsers(pagination.pageNumber + 1)} 
            disabled={pagination.pageNumber === pagination.totalPages || loading}
            style={{ padding: '8px 16px', borderRadius: 6, border: '1px solid #ddd', background: '#fff', cursor: pagination.pageNumber === pagination.totalPages ? 'not-allowed' : 'pointer' }}
          >
            {t('common.next')} →
          </button>
        </div>
      )}
    </div>
  );
}

export default AdminUsersPage;