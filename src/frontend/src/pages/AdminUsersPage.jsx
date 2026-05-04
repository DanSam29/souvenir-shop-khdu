import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { adminUsersAPI } from '../services/api';

function AdminUsersPage() {
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [updating, setUpdating] = useState(false);

  useEffect(() => {
    loadUsers();
  }, []);

  const loadUsers = async () => {
    try {
      setLoading(true);
      const res = await adminUsersAPI.getAll();
      setUsers(res.data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleRoleChange = async (id, newRole) => {
    try {
      setUpdating(true);
      await adminUsersAPI.updateRole(id, { role: newRole });
      alert('Роль оновлено');
      loadUsers();
    } catch (err) {
      alert('Помилка при оновленні ролі');
    } finally {
      setUpdating(false);
    }
  };

  if (loading) return <div className="loading">Завантаження...</div>;

  return (
    <div className="admin-users" style={{ padding: 20, maxWidth: 1400, margin: '0 auto' }}>
      <div style={{ marginBottom: 20 }}>
        <Link to="/admin" className="back-link">
          ← Назад до дашборду
        </Link>
        <h1>Керування користувачами</h1>
      </div>
      <div style={{ background: '#fff', borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.05)', overflow: 'hidden' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead style={{ background: '#f8f9fa' }}>
            <tr>
              <th style={{ padding: '16px', textAlign: 'left', borderBottom: '1px solid #eee' }}>ID</th>
              <th style={{ padding: '16px', textAlign: 'left', borderBottom: '1px solid #eee' }}>Ім'я</th>
              <th style={{ padding: '16px', textAlign: 'left', borderBottom: '1px solid #eee' }}>Прізвище</th>
              <th style={{ padding: '16px', textAlign: 'left', borderBottom: '1px solid #eee' }}>Email</th>
              <th style={{ padding: '16px', textAlign: 'left', borderBottom: '1px solid #eee' }}>Роль</th>
              <th style={{ padding: '16px', textAlign: 'left', borderBottom: '1px solid #eee' }}>Студентський статус</th>
              <th style={{ padding: '16px', textAlign: 'left', borderBottom: '1px solid #eee' }}>Дата реєстрації</th>
              <th style={{ padding: '16px', textAlign: 'left', borderBottom: '1px solid #eee' }}>Дії</th>
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
            Користувачів ще немає
          </div>
        )}
      </div>
    </div>
  );
}

export default AdminUsersPage;
