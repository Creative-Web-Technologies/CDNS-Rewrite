import { useState, useEffect, useCallback } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { Plus, Search, X, ClipboardList, AlertCircle, RefreshCw } from 'lucide-react';
import { Button, Card, DataTable, EmptyState, PageTransition, Pill } from '../../../components/ui';
import { usePortalChrome } from '../../../components/portal/portalChrome';
import { apiGet } from '../../../lib/api';
import './master-requests.css';

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

export default function MasterRequestsPage() {
  const navigate = useNavigate();
  const [status, setStatus] = useState('');
  const [requestNumber, setRequestNumber] = useState('');
  const [memberEmail, setMemberEmail] = useState('');
  const [search, setSearch] = useState('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');

  const [appliedFilters, setAppliedFilters] = useState({
    status: '', requestNumber: '', memberEmail: '', search: '', fromDate: '', toDate: '',
  });
  const [page, setPage] = useState(1);
  const pageSize = 20;
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(false);

  usePortalChrome({
    actions: (
      <Button icon={Plus} onClick={() => navigate('/company/requests/new')}>
        New request
      </Button>
    ),
  });

  const load = useCallback(async (filters, p) => {
    setLoading(true);
    setError(false);
    try {
      const params = new URLSearchParams({ page: String(p), pageSize: String(pageSize) });
      if (filters.status !== '') params.set('status', filters.status);
      if (filters.requestNumber) params.set('requestNumber', filters.requestNumber);
      if (filters.memberEmail) params.set('memberEmail', filters.memberEmail);
      if (filters.search) params.set('search', filters.search);
      if (filters.fromDate && filters.toDate) {
        params.set('fromDate', filters.fromDate);
        params.set('toDate', filters.toDate);
      }
      const result = await apiGet(`/api/master/subscription-requests?${params}`);
      setData(result);
    } catch {
      setError(true);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(appliedFilters, page); }, [appliedFilters, page, load]);

  function handleSearch() {
    setAppliedFilters({ status, requestNumber, memberEmail, search, fromDate, toDate });
    setPage(1);
  }

  function handleReset() {
    setStatus('');
    setRequestNumber('');
    setMemberEmail('');
    setSearch('');
    setFromDate('');
    setToDate('');
    setAppliedFilters({ status: '', requestNumber: '', memberEmail: '', search: '', fromDate: '', toDate: '' });
    setPage(1);
  }

  const rows = data?.items ?? [];
  const total = data?.totalCount ?? 0;
  const start = total === 0 ? 0 : (page - 1) * pageSize + 1;
  const end = Math.min(page * pageSize, total);

  const columns = [
    { key: 'requestNumber', header: 'Request no.', render: (r) => r.requestNumber ?? '—' },
    { key: 'memberName', header: 'Member', render: (r) => r.memberName?.trim() || '—' },
    { key: 'memberEmail', header: 'Email' },
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
      render: (r) => <Link to={`/company/requests/${r.subscriptionRequestId}`} className="mr-link">Review</Link>,
    },
  ];

  return (
    <PageTransition>
      <div className="mr-stack">
        <div className="mr-page-header">
          <div>
            <h1 className="mr-page-title">Subscription requests</h1>
            <p className="mr-page-subtitle">Requests from members of companies you administer.</p>
          </div>
          <Button icon={Plus} onClick={() => navigate('/company/requests/new')}>
            New request
          </Button>
        </div>

        <Card title="Filter">
          <div className="mr-filters">
            <div className="mr-filter-row">
              <div className="mr-filter-field">
                <label htmlFor="mr-status" className="mr-label">Status</label>
                <select id="mr-status" className="mr-select" value={status} onChange={(e) => setStatus(e.target.value)}>
                  {STATUS_OPTIONS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
                </select>
              </div>
              <div className="mr-filter-field">
                <label htmlFor="mr-rnum" className="mr-label">Request number</label>
                <input
                  id="mr-rnum"
                  type="text"
                  className="mr-input"
                  placeholder="SR-000001"
                  value={requestNumber}
                  onChange={(e) => setRequestNumber(e.target.value)}
                  onKeyDown={(e) => { if (e.key === 'Enter') handleSearch(); }}
                />
              </div>
              <div className="mr-filter-field">
                <label htmlFor="mr-email" className="mr-label">Member email</label>
                <input
                  id="mr-email"
                  type="text"
                  className="mr-input"
                  placeholder="member@example.com"
                  value={memberEmail}
                  onChange={(e) => setMemberEmail(e.target.value)}
                  onKeyDown={(e) => { if (e.key === 'Enter') handleSearch(); }}
                />
              </div>
              <div className="mr-filter-field">
                <label htmlFor="mr-search" className="mr-label">Free text</label>
                <input
                  id="mr-search"
                  type="text"
                  className="mr-input"
                  placeholder="Search…"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  onKeyDown={(e) => { if (e.key === 'Enter') handleSearch(); }}
                />
              </div>
            </div>
            <div className="mr-filter-row" style={{ marginTop: 10 }}>
              <div className="mr-filter-field">
                <label htmlFor="mr-from" className="mr-label">From date</label>
                <input
                  id="mr-from"
                  type="date"
                  className="mr-input"
                  value={fromDate}
                  onChange={(e) => setFromDate(e.target.value)}
                />
              </div>
              <div className="mr-filter-field">
                <label htmlFor="mr-to" className="mr-label">To date</label>
                <input
                  id="mr-to"
                  type="date"
                  className="mr-input"
                  value={toDate}
                  onChange={(e) => setToDate(e.target.value)}
                />
              </div>
              <div className="mr-filter-actions">
                <Button icon={Search} onClick={handleSearch}>Search</Button>
                <Button variant="secondary" icon={X} onClick={handleReset}>Reset</Button>
              </div>
            </div>
          </div>
        </Card>

        <Card
          title="Member subscription requests"
          subtitle="Sorted by newest first"
          actions={!loading && total > 0 ? (
            <span className="mr-count">Showing {start}–{end} of {total}</span>
          ) : undefined}
        >
          <div className="mr-table-wrap">
            {loading ? (
              <div className="mr-loading" aria-busy="true">Loading…</div>
            ) : error ? (
              <EmptyState
                icon={AlertCircle}
                title="Couldn't load requests"
                message="Something went wrong while fetching subscription requests."
                action={
                  <Button variant="secondary" icon={RefreshCw} onClick={() => load(appliedFilters, page)}>
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
                    message="No subscription requests have been received yet, or none match your filters."
                  />
                }
              />
            )}
          </div>

          {total > pageSize && (
            <div className="mr-pagination">
              <Button variant="secondary" size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
                Previous
              </Button>
              <span className="mr-page-info">Page {page}</span>
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
