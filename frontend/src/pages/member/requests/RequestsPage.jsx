import { useState, useEffect, useCallback } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { Plus, Search, X, ClipboardList, AlertCircle, RefreshCw } from 'lucide-react';
import { Button, Card, DataTable, EmptyState, PageTransition, Pill } from '../../../components/ui';
import { usePortalChrome } from '../../../components/portal/portalChrome';
import { apiGet } from '../../../lib/api';
import './requests.css';

const STATUS_OPTIONS = [
  { value: '', label: 'All statuses' },
  { value: '0', label: 'Pending' },
  { value: '1', label: 'Approved' },
  { value: '2', label: 'Rejected' },
  { value: '3', label: 'Cancelled' },
];

function statusTone(status) {
  if (status === 'Pending') return 'amber';
  if (status === 'Approved') return 'green';
  if (status === 'Rejected') return 'red';
  if (status === 'Cancelled') return 'grey';
  return 'grey';
}

function formatDate(iso) {
  if (!iso) return '—';
  return new Date(iso).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
}

export default function RequestsPage() {
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const [appliedSearch, setAppliedSearch] = useState('');
  const [appliedStatus, setAppliedStatus] = useState('');
  const [page, setPage] = useState(1);
  const pageSize = 20;
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(false);

  usePortalChrome({
    actions: (
      <Button icon={Plus} onClick={() => navigate('/member/requests/new')}>
        New request
      </Button>
    ),
  });

  const load = useCallback(async (s, st, p) => {
    setLoading(true);
    setError(false);
    try {
      const params = new URLSearchParams({ page: String(p), pageSize: String(pageSize) });
      if (s) params.set('search', s);
      if (st !== '') params.set('status', st);
      const result = await apiGet(`/api/member/subscription-requests?${params}`);
      setData(result);
    } catch {
      setError(true);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(appliedSearch, appliedStatus, page); }, [appliedSearch, appliedStatus, page, load]);

  function handleSearch() {
    setAppliedSearch(search);
    setAppliedStatus(status);
    setPage(1);
  }

  function handleReset() {
    setSearch('');
    setStatus('');
    setAppliedSearch('');
    setAppliedStatus('');
    setPage(1);
  }

  const rows = data?.items ?? [];
  const total = data?.totalCount ?? 0;
  const start = total === 0 ? 0 : (page - 1) * pageSize + 1;
  const end = Math.min(page * pageSize, total);

  const columns = [
    { key: 'requestNumber', header: 'Request no.', render: (r) => r.requestNumber ?? '—' },
    { key: 'companyName', header: 'Company' },
    { key: 'companyTin', header: 'TIN', render: (r) => r.companyTin ?? '—' },
    {
      key: 'subscriptionType', header: 'Type',
      render: (r) => (
        r.subscriptionType === 'Declarant'
          ? <span className="sub-pill sub-pill--declarant">{r.subscriptionType}</span>
          : <Pill tone="blue">{r.subscriptionType}</Pill>
      ),
    },
    { key: 'requestedDateTime', header: 'Requested', render: (r) => formatDate(r.requestedDateTime) },
    {
      key: 'status', header: 'Status',
      render: (r) => <Pill tone={statusTone(r.status)}>{r.status}</Pill>,
    },
    {
      key: 'view', header: '', align: 'right',
      render: (r) => <Link to={`/member/requests/${r.subscriptionRequestId}`} className="req-link">View</Link>,
    },
  ];

  return (
    <PageTransition>
      <div className="req-stack">
        <div className="req-page-header">
          <div>
            <h1 className="req-page-title">Subscription requests</h1>
            <p className="req-page-subtitle">Requests you have sent to companies.</p>
          </div>
          <Button icon={Plus} onClick={() => navigate('/member/requests/new')}>
            New request
          </Button>
        </div>

        <Card title="Filter">
          <div className="req-filters">
            <div className="req-filter-row">
              <div className="req-filter-field">
                <label htmlFor="req-search" className="req-label">Company or request no.</label>
                <input
                  id="req-search"
                  type="text"
                  className="req-input"
                  placeholder="Search…"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  onKeyDown={(e) => { if (e.key === 'Enter') handleSearch(); }}
                />
              </div>
              <div className="req-filter-field">
                <label htmlFor="req-status" className="req-label">Status</label>
                <select id="req-status" className="req-select" value={status} onChange={(e) => setStatus(e.target.value)}>
                  {STATUS_OPTIONS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
                </select>
              </div>
              <div className="req-filter-actions">
                <Button icon={Search} onClick={handleSearch}>Search</Button>
                <Button variant="secondary" icon={X} onClick={handleReset}>Reset</Button>
              </div>
            </div>
          </div>
        </Card>

        <Card
          title="My subscription requests"
          subtitle="Sorted by status, then newest"
          actions={!loading && total > 0 ? (
            <span className="req-count">Showing {start}–{end} of {total}</span>
          ) : undefined}
        >
          <div className="req-table-wrap">
            {loading ? (
              <div className="req-loading" aria-busy="true">Loading…</div>
            ) : error ? (
              <EmptyState
                icon={AlertCircle}
                title="Couldn't load requests"
                message="Something went wrong while fetching subscription requests."
                action={
                  <Button variant="secondary" icon={RefreshCw} onClick={() => load(appliedSearch, appliedStatus, page)}>
                    Retry
                  </Button>
                }
              />
            ) : (
              <DataTable
                columns={columns}
                rows={rows}
                rowKey={(r) => r.subscriptionRequestId}
                empty={
                  <EmptyState
                    icon={ClipboardList}
                    title="No requests found"
                    message="You haven't sent any subscription requests yet, or none match your filters."
                    action={<Button icon={Plus} onClick={() => navigate('/member/requests/new')}>New request</Button>}
                  />
                }
              />
            )}
          </div>

          {total > pageSize && (
            <div className="req-pagination">
              <Button variant="secondary" size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
                Previous
              </Button>
              <span className="req-page-info">Page {page}</span>
              <Button variant="secondary" size="sm" disabled={end >= total} onClick={() => setPage((p) => p + 1)}>
                Next
              </Button>
            </div>
          )}
        </Card>
      </div>
    </PageTransition>
  );
}
