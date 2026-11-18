import React, { useMemo, useState } from 'react';
import Spinner from './Spinner';

export default function DataTable({
  columns = [],
  data = [],
  keyField = 'id',
  pageSize = 10,
  loading = false,
  emptyMessage = 'Немає даних',
  toolbar = null
}) {
  const [sort, setSort] = useState({ key: null, dir: 'asc' });
  const [page, setPage] = useState(1);

  const sorted = useMemo(() => {
    if (!sort.key) return data;
    const arr = [...data];
    arr.sort((a, b) => {
      const av = a[sort.key];
      const bv = b[sort.key];
      if (av == null) return 1;
      if (bv == null) return -1;
      if (typeof av === 'number' && typeof bv === 'number') {
        return sort.dir === 'asc' ? av - bv : bv - av;
      }
      return sort.dir === 'asc'
        ? String(av).localeCompare(String(bv))
        : String(bv).localeCompare(String(av));
    });
    return arr;
  }, [data, sort]);

  const totalPages = Math.max(1, Math.ceil(sorted.length / pageSize));
  const currentPage = Math.min(page, totalPages);
  const paged = sorted.slice((currentPage - 1) * pageSize, currentPage * pageSize);

  const toggleSort = (key) => {
    setSort((prev) => (prev.key === key ? { key, dir: prev.dir === 'asc' ? 'desc' : 'asc' } : { key, dir: 'asc' }));
    setPage(1);
  };

  return (
    <div className="w-full">
      {toolbar && <div className="mb-3">{toolbar}</div>}
      <div className="overflow-x-auto rounded-xl border border-ink/10 bg-white">
        <table className="w-full border-collapse text-sm">
          <thead className="bg-ink/5 text-left text-ink">
            <tr>
              {columns.map((col) => (
                <th key={col.key} className={`whitespace-nowrap px-4 py-3 font-semibold ${col.className || ''}`}>
                  {col.sortable ? (
                    <button
                      type="button"
                      onClick={() => toggleSort(col.key)}
                      className="inline-flex items-center gap-1 hover:text-brand"
                      aria-label={`Сортувати за ${col.header}`}
                    >
                      {col.header}
                      <span aria-hidden="true" className="text-xs">
                        {sort.key === col.key ? (sort.dir === 'asc' ? '▲' : '▼') : '↕'}
                      </span>
                    </button>
                  ) : (
                    col.header
                  )}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={columns.length || 1} className="px-4 py-10 text-center">
                  <Spinner />
                </td>
              </tr>
            ) : paged.length === 0 ? (
              <tr>
                <td colSpan={columns.length || 1} className="px-4 py-10 text-center text-ink-light">
                  {emptyMessage}
                </td>
              </tr>
            ) : (
              paged.map((row, idx) => (
                <tr key={row[keyField] ?? idx} className="border-t border-ink/10 hover:bg-brand/5">
                  {columns.map((col) => (
                    <td key={col.key} className={`px-4 py-3 align-middle ${col.className || ''}`}>
                      {col.render ? col.render(row) : row[col.key]}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
      {totalPages > 1 && (
        <div className="mt-3 flex items-center justify-between text-sm text-ink-light">
          <span>
            Сторінка {currentPage} з {totalPages}
          </span>
          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={currentPage === 1}
              className="rounded-lg border border-ink/20 px-3 py-1 disabled:cursor-not-allowed disabled:opacity-50 hover:bg-ink/5"
            >
              Назад
            </button>
            <button
              type="button"
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              disabled={currentPage === totalPages}
              className="rounded-lg border border-ink/20 px-3 py-1 disabled:cursor-not-allowed disabled:opacity-50 hover:bg-ink/5"
            >
              Далі
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
