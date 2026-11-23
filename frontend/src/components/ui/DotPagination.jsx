import { ChevronLeft, ChevronRight } from 'lucide-react';
import './dot-pagination.css';

function getDotWindow(currentPage, totalPages, maxDots = 9) {
  if (totalPages <= maxDots) {
    return Array.from({ length: totalPages }, (_, i) => i + 1);
  }
  const half = Math.floor(maxDots / 2);
  const start = Math.max(1, Math.min(currentPage - half, totalPages - maxDots + 1));
  const end = Math.min(totalPages, start + maxDots - 1);
  return Array.from({ length: end - start + 1 }, (_, i) => start + i);
}

export default function DotPagination({ page, totalCount, pageSize, onPageChange }) {
  const totalPages = Math.ceil(totalCount / pageSize);
  if (totalCount <= pageSize) return null;

  const start = (page - 1) * pageSize + 1;
  const end = Math.min(page * pageSize, totalCount);
  const dots = getDotWindow(page, totalPages);

  return (
    <nav className="dp-nav" aria-label="Pagination">
      <span className="dp-info">Showing {start}–{end} of {totalCount}</span>

      <div className="dp-controls">
        <button
          type="button"
          className="dp-arrow"
          aria-label="Previous page"
          disabled={page <= 1}
          onClick={() => onPageChange(page - 1)}
        >
          <ChevronLeft size={16} aria-hidden="true" />
        </button>

        <div className="dp-dots" role="list">
          {dots.map((n) => (
            <button
              key={n}
              type="button"
              role="listitem"
              className={`dp-dot${n === page ? ' dp-dot--active' : ''}`}
              aria-label={`Page ${n}`}
              aria-current={n === page ? 'page' : undefined}
              title={`Page ${n}`}
              onClick={() => onPageChange(n)}
            />
          ))}
        </div>

        <button
          type="button"
          className="dp-arrow"
          aria-label="Next page"
          disabled={page >= totalPages}
          onClick={() => onPageChange(page + 1)}
        >
          <ChevronRight size={16} aria-hidden="true" />
        </button>
      </div>

      <span className="dp-info">Page {page} of {totalPages}</span>
    </nav>
  );
}
