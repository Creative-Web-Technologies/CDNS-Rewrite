import { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { Search, X, BadgeCheck, AlertCircle, RefreshCw } from 'lucide-react';
import { Button, Card, DataTable, EmptyState, PageTransition, Pill } from '../../../components/ui';
import { usePortalChrome } from '../../../components/portal/portalChrome';
import { apiGet } from '../../../lib/api';
import './master-subscriptions.css';

const STATUS_OPTIONS = [
  { value: '', label: 'All statuses' },
  { value: '1', label: 'Active' },
  { value: '2', label: 'Deactivated' },
  { value: '3', label: 'Suspended' },
  { value: '4', label: 'Expired' },
  { value: '0', label: 'Payment pending' },
];

function statusTone(status) {
  if (status === 'Active') return 'green';
  if (status === 'Deactivated') return 'red';
  if (status === 'Expired') return 'grey';
  if (status === 'Suspended') return 'amber';
  return 'grey';
}

function formatDate(iso) {
  if (!iso) return '—';
  return new Date(iso).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
}

export default function MasterSubscriptionsPage() {
  const [status, setStatus] = useState('');
  const [subscriptionNumber, setSubscriptionNumber] = useState('');
  const [memberEmail, setMemberEmail] = useState('');
  const [search, setSearch] = useState('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');

  const [appliedFilters, setAppliedFilters] = useState({
    status: '', subscriptionNumber: '', memberEmail: '', search: '', fromDate: '', toDate: '',
  });
  const [page, setPage] = useState(1);
  const pageSize = 20;
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(false);

  usePortalChrome({});

  const load = useCallback(async (filters, p) => {
    setLoading(true);
    setError(false);
    try {
      const params = new URLSearchParams({ page: String(p), pageSize: String(pageSize) });
      if (filters.status !== '') params.set('status', filters.status);
      if (filters.subscriptionNumber) params.set('subscriptionNumber', filters.subscriptionNumber);
      if (filters.memberEmail) params.set('memberEmail', filters.memberEmail);
      if (filters.search) params.set('search', filters.search);
      if (filters.fromDate && filters.toDate) {
        params.set('fromDate', filters.fromDate);
        params.set('toDate', filters.toDate);
      }
      const result = await apiGet(`/api/master/subscriptions?${params}`);
      setData(result);
    } catch {
      setError(true);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(appliedFilters, page); }, [appliedFilters, page, load]);

  function handleSearch() {
    setAppliedFilters({ status, subscriptionNumber, memberEmail, search, fromDate, toDate });
    setPage(1);
  }

  function handleReset() {
    setStatus('');
    setSubscriptionNumber('');
    setMemberEmail('');
    setSearch('');
    setFromDate('');
    setToDate('');
    setAppliedFilters({ status: '', subscriptionNumber: '', memberEmail: '', search: '', fromDate: '', toDate: '' });
    setPage(1);
  }

  const rows = data?.items ?? [];
  const total = data?.totalCount ?? 0;
  const start = total === 0 ? 0 : (page - 1) * pageSize + 1;
  const end = Math.min(page * pageSize, total);

  const columns = [
    { key: 'subscriptionNumber', header: 'Sub. no.', render: (r) => r.subscriptionNumber ?? '—' },
    { key: 'memberName', header: 'Member', render: (r) => r.memberName?.trim() || '—' },
    { key: 'memberEmail', header: 'Email' },
    { key: 'subscriptionName', header: 'Plan' },
    {
      key: 'subscriptionType', header: 'Type',
      render: (r) => (
        r.subscriptionType === 'Declarant'
          ? <span className="sub-pill sub-pill--declarant">{r.subscriptionType}</span>
          : <Pill tone="blue">{r.subscriptionType}</Pill>
      ),
    },
    { key: 'startDate', header: 'Start', render: (r) => formatDate(r.startDate) },
    { key: 'expiryDate', header: 'Expiry', render: (r) => formatDate(r.expiryDate) },
    {
      key: 'status', header: 'Status',
      render: (r) => <Pill tone={statusTone(r.status)}>{r.status}</Pill>,
    },
    {
      key: 'view', header: '', align: 'right',
      render: (r) => <Link to={`/company/subscriptions/${r.memberSubscriptionId}`} className="ms-link">View</Link>,
    },
  ];

  return (
    <PageTransition>
      <div className="ms-stack">
        <div className="ms-page-header">
          <div>
            <h1 className="ms-page-title">Subscriptions</h1>
            <p className="ms-page-subtitle">Subscriptions you have issued to members.</p>
          </div>
        </div>

        <Card title="Filter">
          <div className="ms-filters">
            <div className="ms-filter-row">
              <div className="ms-filter-field">
                <label htmlFor="ms-status" className="ms-label">Status</label>
                <select id="ms-status" className="ms-select" value={status} onChange={(e) => setStatus(e.target.value)}>
                  {STATUS_OPTIONS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
                </select>
              </div>
              <div className="ms-filter-field">
                <label htmlFor="ms-subnum" className="ms-label">Subscription number</label>
                <input
                  id="ms-subnum"
                  type="text"
                  className="ms-input"
                  placeholder="MS-000001"
                  value={subscriptionNumber}
                  onChange={(e) => setSubscriptionNumber(e.target.value)}
                  onKeyDown={(e) => { if (e.key === 'Enter') handleSearch(); }}
                />
              </div>
              <div className="ms-filter-field">
                <label htmlFor="ms-email" className="ms-label">Member email</label>
                <input
                  id="ms-email"
                  type="text"
                  className="ms-input"
                  placeholder="member@example.com"
                  value={memberEmail}
                  onChange={(e) => setMemberEmail(e.target.value)}
                  onKeyDown={(e) => { if (e.key === 'Enter') handleSearch(); }}
                />
              </div>
              <div className="ms-filter-field">
                <label htmlFor="ms-search" className="ms-label">Free text</label>
                <input
                  id="ms-search"
                  type="text"
                  className="ms-input"
                  placeholder="Search…"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  onKeyDown={(e) => { if (e.key === 'Enter') handleSearch(); }}
                />
              </div>
            </div>
            <div className="ms-filter-row" style={{ marginTop: 10 }}>
              <div className="ms-filter-field">
                <label htmlFor="ms-from" className="ms-label">From date</label>
                <input
                  id="ms-from"
                  type="date"
                  className="ms-input"
                  value={fromDate}
                  onChange={(e) => setFromDate(e.target.value)}
                />
              </div>
              <div className="ms-filter-field">
                <label htmlFor="ms-to" className="ms-label">To date</label>
                <input
                  id="ms-to"
                  type="date"
                  className="ms-input"
                  value={toDate}
                  onChange={(e) => setToDate(e.target.value)}
                />
              </div>
              <div className="ms-filter-actions">
                <Button icon={Search} onClick={handleSearch}>Search</Button>
                <Button variant="secondary" icon={X} onClick={handleReset}>Reset</Button>
              </div>
            </div>
          </div>
        </Card>

        <Card
          title="Issued subscriptions"
          subtitle="Sorted by expiry date, then status"
          actions={!loading && total > 0 ? (
            <span className="ms-count">Showing {start}–{end} of {total}</span>
          ) : undefined}
        >
          <div className="ms-table-wrap">
            {loading ? (
              <div className="ms-loading" aria-busy="true">Loading…</div>
            ) : error ? (
              <EmptyState
                icon={AlertCircle}
                title="Couldn't load subscriptions"
                message="Something went wrong while fetching subscriptions."
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
                rowKey={(r) => r.memberSubscriptionId}
                empty={
                  <EmptyState
                    icon={BadgeCheck}
                    title="No subscriptions found"
                    message="No subscriptions have been issued yet, or none match your filters."
                  />
                }
              />
            )}
          </div>

          {total > pageSize && (
            <div className="ms-pagination">
              <Button variant="secondary" size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
                Previous
              </Button>
              <span className="ms-page-info">Page {page}</span>
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
