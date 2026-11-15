import EmptyState from './EmptyState';

// DataTable — a simple, presentational table that scrolls horizontally on
// narrow screens. `columns` is [{ key, header, render?, align?, width? }];
// `rows` is the data; `rowKey` maps a row to a stable key.
export default function DataTable({
  columns,
  rows,
  rowKey,
  empty,
  className = '',
}) {
  if (!rows || rows.length === 0) {
    return empty ?? <EmptyState title="No records" />;
  }

  return (
    <div className={`ui-table-scroll ${className}`}>
      <table className="ui-table">
        <thead>
          <tr>
            {columns.map((col) => (
              <th
                key={col.key}
                style={{ textAlign: col.align || 'left', width: col.width }}
                scope="col"
              >
                {col.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, i) => (
            <tr key={rowKey ? rowKey(row, i) : i}>
              {columns.map((col) => (
                <td key={col.key} style={{ textAlign: col.align || 'left' }}>
                  {col.render ? col.render(row, i) : row[col.key]}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
