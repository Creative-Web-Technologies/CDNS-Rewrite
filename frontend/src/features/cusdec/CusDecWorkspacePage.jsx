import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { FileText, AlertCircle, RefreshCw, ShieldOff } from 'lucide-react';
import { Button, Card, EmptyState, PageTransition } from '../../components/ui';
import DotPagination from '../../components/ui/DotPagination';
import { usePortalChrome } from '../../components/portal/portalChrome';
import { useToast } from '../../components/ui/Toast';
import { useDebounce } from '../../lib/useDebounce';
import {
  getStrip, getOffices, getSubscriptions,
  acknowledge, disclaim, complete, reopen,
} from './cusdecApi';
import { useCusDecList } from './useCusDecList';
import { isGateError, gateBlockedInfo, cusDecErrorMessage } from './errorMessages';
import SubscriptionStrip from './SubscriptionStrip';
import FilterBar from './FilterBar';
import CusDecCard from './CusDecCard';
import DisclaimDialog from './DisclaimDialog';
import './cusdec.css';

const PAGE_SIZE = 4;

const TABS = [
  { key: 'tracking', label: 'Tracking' },
  { key: 'completed', label: 'Completed' },
  { key: 'disclaimed', label: 'Disclaimed' },
];

const INITIAL_FILTERS = {
  search: '', cusDecNumber: '', counterpartyTin: '',
  office: '', fromDate: '', toDate: '', sort: 'newest',
};

function tabCount(counts, tab) {
  if (!counts) return null;
  if (tab === 'tracking') return counts.trackingNew + counts.trackingInProgress;
  if (tab === 'completed') return counts.completed;
  if (tab === 'disclaimed') return counts.disclaimed;
  return null;
}

function sortLabel(tab) {
  if (tab === 'completed') return 'Completed date';
  if (tab === 'disclaimed') return 'Disclaimed date';
  return 'Registered date';
}

function SkeletonCard() {
  return (
    <article className="cd-row cd-row--skeleton" aria-hidden="true">
      <div className="cd-row__identity">
        <div className="cd-skel cd-skel--title" />
        <div className="cd-skel cd-skel--line" style={{ marginTop: 6 }} />
      </div>
      <div className="cd-row__party">
        <div className="cd-skel cd-skel--row" />
        <div className="cd-skel cd-skel--line" style={{ marginTop: 6 }} />
      </div>
      <div className="cd-row__status">
        <div className="cd-skel cd-skel--row" style={{ width: 70 }} />
      </div>
      <div className="cd-row__actions">
        <div className="cd-skel cd-skel--actions" />
      </div>
    </article>
  );
}

export default function CusDecWorkspacePage({ portal }) {
  const { subscriptionId } = useParams();
  const subId = parseInt(subscriptionId, 10) || null;
  const navigate = useNavigate();
  const { addToast } = useToast();

  const [searchParams, setSearchParams] = useSearchParams();
  const tab = searchParams.get('tab') || 'tracking';
  const page = Math.max(1, parseInt(searchParams.get('page') || '1', 10));

  const subsLink = `/${portal}/subscriptions`;
  const pickerLink = `/${portal}/cusdecs`;

  const [trackingState, setTrackingState] = useState('all');
  const [filters, setFilters] = useState(INITIAL_FILTERS);

  const debouncedSearch = useDebounce(filters.search, 300);
  const debouncedCusDecNumber = useDebounce(filters.cusDecNumber, 300);
  const debouncedTin = useDebounce(filters.counterpartyTin, 300);

  const appliedFilters = useMemo(() => ({
    search: debouncedSearch,
    cusDecNumber: debouncedCusDecNumber,
    counterpartyTin: debouncedTin,
    office: filters.office,
    fromDate: filters.fromDate,
    toDate: filters.toDate,
    sort: filters.sort,
  }), [debouncedSearch, debouncedCusDecNumber, debouncedTin, filters.office, filters.fromDate, filters.toDate, filters.sort]);

  const mountedRef = useRef(false);
  const prevAppliedRef = useRef(appliedFilters);
  useEffect(() => {
    if (!mountedRef.current) { mountedRef.current = true; return; }
    if (prevAppliedRef.current === appliedFilters) return;
    prevAppliedRef.current = appliedFilters;
    setSearchParams((prev) => {
      const next = new URLSearchParams(prev);
      if (next.get('page') !== '1') { next.set('page', '1'); return next; }
      return prev;
    }, { replace: true });
  }, [appliedFilters, setSearchParams]);

  const [strip, setStrip] = useState(null);
  const [stripLoading, setStripLoading] = useState(false);
  const [stripError, setStripError] = useState(null);
  const [subscriptions, setSubscriptions] = useState([]);
  const [offices, setOffices] = useState([]);
  const [gateBlocked, setGateBlocked] = useState(null);
  const [busyCardId, setBusyCardId] = useState(null);
  const [highlightedCardId, setHighlightedCardId] = useState(null);
  const [disclaimTarget, setDisclaimTarget] = useState(null);
  const [disclaimBusy, setDisclaimBusy] = useState(false);

  usePortalChrome({ title: 'CusDecs' });

  const handleGateError = useCallback((code) => {
    if (code === 'SubscriptionNotFound') {
      navigate(pickerLink, { replace: true });
      addToast({ type: 'error', message: 'That subscription isn\'t available.' });
    } else if (isGateError(code)) {
      setGateBlocked(gateBlockedInfo(code));
    }
  }, [navigate, pickerLink, addToast]);

  const refetchStrip = useCallback(async () => {
    if (!subId) return;
    try {
      const data = await getStrip(subId);
      setStrip(data);
    } catch (err) {
      const code = err?.code;
      if (isGateError(code) || code === 'SubscriptionNotFound') handleGateError(code);
    }
  }, [subId, handleGateError]);

  useEffect(() => {
    if (!subId) {
      navigate(pickerLink, { replace: true });
    }
  }, [subId, pickerLink, navigate]);

  useEffect(() => {
    if (!subId) return;
    let alive = true;
    setStrip(null);
    setStripLoading(true);
    setStripError(null);
    setGateBlocked(null);
    setOffices([]);
    setTrackingState('all');
    setFilters(INITIAL_FILTERS);
    setSearchParams({ tab: 'tracking', page: '1' }, { replace: true });

    Promise.all([
      getStrip(subId),
      getOffices(subId).catch(() => []),
      getSubscriptions().catch(() => []),
    ])
      .then(([stripData, officeList, subList]) => {
        if (!alive) return;
        setStrip(stripData);
        setOffices(officeList ?? []);
        setSubscriptions(subList ?? []);
      })
      .catch((err) => {
        if (!alive) return;
        const code = err.code;
        if (isGateError(code) || code === 'SubscriptionNotFound') {
          handleGateError(code);
        } else {
          setStripError(err);
        }
      })
      .finally(() => { if (alive) setStripLoading(false); });

    return () => { alive = false; };
  }, [subId, handleGateError, setSearchParams]);

  const {
    items, setItems, totalCount,
    loading: listLoading, error: listError, refetch: refetchList,
  } = useCusDecList({
    subId,
    tab,
    trackingState,
    filters: appliedFilters,
    page,
    pageSize: PAGE_SIZE,
    subscriptionType: strip?.subscriptionType,
  });

  useEffect(() => {
    if (!listError) return;
    const code = listError?.code;
    if (isGateError(code) || code === 'SubscriptionNotFound') {
      handleGateError(code);
    }
  }, [listError, handleGateError]);

  useEffect(() => {
    if (!highlightedCardId) return;
    const t = setTimeout(() => setHighlightedCardId(null), 1800);
    return () => clearTimeout(t);
  }, [highlightedCardId]);

  function setPage(n) {
    setSearchParams((prev) => {
      const next = new URLSearchParams(prev);
      next.set('page', String(n));
      return next;
    });
  }

  function handleTabChange(newTab) {
    setFilters(INITIAL_FILTERS);
    setTrackingState('all');
    setSearchParams({ tab: newTab, page: '1' });
  }

  // ARIA tab pattern: arrow keys move focus and activate the tab
  function handleTabKeyDown(e, currentKey) {
    const idx = TABS.findIndex((t) => t.key === currentKey);
    let newIdx = -1;
    if (e.key === 'ArrowRight') newIdx = (idx + 1) % TABS.length;
    else if (e.key === 'ArrowLeft') newIdx = (idx - 1 + TABS.length) % TABS.length;
    else if (e.key === 'Home') newIdx = 0;
    else if (e.key === 'End') newIdx = TABS.length - 1;
    else return;
    e.preventDefault();
    handleTabChange(TABS[newIdx].key);
    document.getElementById(`cd-tab-${TABS[newIdx].key}`)?.focus();
  }

  function handleFilterChange(key, value) {
    setFilters((prev) => ({ ...prev, [key]: value }));
  }

  function handleClearFilters() {
    setFilters(INITIAL_FILTERS);
    setTrackingState('all');
    setSearchParams((prev) => {
      const next = new URLSearchParams(prev);
      next.set('page', '1');
      return next;
    }, { replace: true });
  }

  function itemBelongsInView(it) {
    if (it.tab !== tab) return false;
    if (tab === 'tracking' && trackingState !== 'all' && it.state !== trackingState) return false;
    return true;
  }

  async function afterAction(cusDecId, updatedItem, actionLabel) {
    if (itemBelongsInView(updatedItem)) {
      setItems((prev) => prev.map((it) => it.cusDecId === cusDecId ? updatedItem : it));
      setHighlightedCardId(cusDecId);
    } else {
      setItems((prev) => prev.filter((it) => it.cusDecId !== cusDecId));
      if (items.length === 1 && page > 1) setPage(page - 1);
    }
    addToast({ type: 'success', message: `${updatedItem.cusDecNumber || 'CusDec'} ${actionLabel}.` });
    refetchStrip();
    setTimeout(() => refetchList(), 400);
  }

  async function handleAcknowledge(cusDecId) {
    if (busyCardId !== null) return;
    setBusyCardId(cusDecId);
    try {
      const result = await acknowledge(subId, cusDecId);
      await afterAction(cusDecId, result, 'acknowledged');
    } catch (err) {
      const code = err.code;
      if (isGateError(code) || code === 'SubscriptionNotFound') {
        handleGateError(code);
      } else if (err.status === 409 || code === 'CusDecNotFound') {
        addToast({ type: 'error', message: cusDecErrorMessage(code) });
        refetchStrip();
        refetchList();
      } else {
        addToast({ type: 'error', message: 'Something went wrong. Please try again.' });
      }
    } finally {
      setBusyCardId(null);
    }
  }

  async function handleComplete(cusDecId) {
    if (busyCardId !== null) return;
    setBusyCardId(cusDecId);
    try {
      const result = await complete(subId, cusDecId);
      await afterAction(cusDecId, result, 'completed');
    } catch (err) {
      const code = err.code;
      if (isGateError(code) || code === 'SubscriptionNotFound') {
        handleGateError(code);
      } else if (err.status === 409 || code === 'CusDecNotFound') {
        addToast({ type: 'error', message: cusDecErrorMessage(code) });
        refetchStrip();
        refetchList();
      } else {
        addToast({ type: 'error', message: 'Something went wrong. Please try again.' });
      }
    } finally {
      setBusyCardId(null);
    }
  }

  async function handleReopen(cusDecId) {
    if (busyCardId !== null) return;
    setBusyCardId(cusDecId);
    try {
      const result = await reopen(subId, cusDecId);
      await afterAction(cusDecId, result, 'reopened');
    } catch (err) {
      const code = err.code;
      if (isGateError(code) || code === 'SubscriptionNotFound') {
        handleGateError(code);
      } else if (err.status === 409 || code === 'CusDecNotFound') {
        addToast({ type: 'error', message: cusDecErrorMessage(code) });
        refetchStrip();
        refetchList();
      } else {
        addToast({ type: 'error', message: 'Something went wrong. Please try again.' });
      }
    } finally {
      setBusyCardId(null);
    }
  }

  async function handleDisclaimConfirm() {
    if (!disclaimTarget) return;
    const { cusDecId } = disclaimTarget;
    setDisclaimBusy(true);
    try {
      const result = await disclaim(subId, cusDecId);
      setDisclaimTarget(null);
      await afterAction(cusDecId, result, 'disclaimed');
    } catch (err) {
      const code = err.code;
      setDisclaimTarget(null);
      if (isGateError(code) || code === 'SubscriptionNotFound') {
        handleGateError(code);
      } else if (err.status === 409 || code === 'CusDecNotFound') {
        addToast({ type: 'error', message: cusDecErrorMessage(code) });
        refetchStrip();
        refetchList();
      } else {
        addToast({ type: 'error', message: 'Something went wrong. Please try again.' });
      }
    } finally {
      setDisclaimBusy(false);
    }
  }

  // ── Render ──────────────────────────────────────────────────────────────────

  if (!subId) return null;

  const counts = strip?.counts;
  const hasFilters = Object.entries(appliedFilters).some(([k, v]) => k !== 'sort' ? v : v !== 'newest');

  return (
    <PageTransition>
      <div className="cd-stack">
        {/* Strip — skeleton / error / loaded */}
        {stripLoading && (
          <div className="cd-strip cd-strip--skeleton" aria-busy="true">
            <div className="cd-skel cd-skel--title" />
            <div className="cd-skel cd-skel--line" />
          </div>
        )}
        {stripError && !gateBlocked && (
          <Card>
            <EmptyState
              icon={AlertCircle}
              title="Couldn't load subscription details"
              message="Something went wrong. Please try again."
              action={
                <Button variant="secondary" icon={RefreshCw} onClick={() => window.location.reload()}>
                  Retry
                </Button>
              }
            />
          </Card>
        )}
        {strip && !gateBlocked && (
          <SubscriptionStrip
            strip={strip}
            subscriptions={subscriptions}
            portal={portal}
          />
        )}

        {/* Gate blocked */}
        {gateBlocked && (
          <Card>
            <EmptyState
              icon={ShieldOff}
              title={gateBlocked.title}
              message={gateBlocked.message}
              action={
                <Button onClick={() => navigate(subsLink)}>
                  Back to subscriptions
                </Button>
              }
            />
          </Card>
        )}

        {!gateBlocked && strip && (
          <>
            {/* Tab bar — on page background */}
            <div className="cd-tabbar" role="tablist" aria-label="Declaration status">
              {TABS.map(({ key, label }) => {
                const n = tabCount(counts, key);
                return (
                  <button
                    key={key}
                    type="button"
                    role="tab"
                    id={`cd-tab-${key}`}
                    aria-selected={tab === key}
                    aria-controls={`cd-tabpanel-${key}`}
                    tabIndex={tab === key ? 0 : -1}
                    className={`cd-tab${tab === key ? ' cd-tab--active' : ''}`}
                    onClick={() => handleTabChange(key)}
                    onKeyDown={(e) => handleTabKeyDown(e, key)}
                  >
                    {label}
                    {n !== null && (
                      <span className="cd-tab-count" aria-hidden="true">{n}</span>
                    )}
                  </button>
                );
              })}
            </div>

            {/* Tab panel */}
            <div
              role="tabpanel"
              id={`cd-tabpanel-${tab}`}
              aria-labelledby={`cd-tab-${tab}`}
              className="cd-tabpanel-wrapper"
            >
              {/* Filter area — 3 blocks on page background */}
              <FilterBar
                tab={tab}
                subscriptionType={strip.subscriptionType}
                offices={offices}
                filters={filters}
                onChange={handleFilterChange}
                onClear={handleClearFilters}
              />

              {/* Sub-bar — on page background, above the list */}
              <div className="cd-subbar">
                <div className="cd-subbar-left">
                  {tab === 'tracking' && (
                    <div className="cd-state-toggle" role="group" aria-label="Filter by state">
                      {[
                        { key: 'all', label: 'All', count: counts ? counts.trackingNew + counts.trackingInProgress : null },
                        { key: 'new', label: 'New', count: counts?.trackingNew ?? null },
                        { key: 'inprogress', label: 'In progress', count: counts?.trackingInProgress ?? null },
                      ].map(({ key, label, count }) => (
                        <button
                          key={key}
                          type="button"
                          className={`cd-state-btn${trackingState === key ? ' cd-state-btn--active' : ''}`}
                          aria-pressed={trackingState === key}
                          onClick={() => { setTrackingState(key); setPage(1); }}
                        >
                          {label}
                          {count !== null && (
                            <span className="cd-state-count" aria-hidden="true">{count}</span>
                          )}
                        </button>
                      ))}
                    </div>
                  )}
                </div>
                <div className="cd-subbar-right">
                  <span className="cd-result-count" aria-live="polite" aria-atomic="true">
                    {!listLoading && `${totalCount} declaration${totalCount !== 1 ? 's' : ''}`}
                  </span>
                  <div className="cd-sort-field">
                    <label htmlFor="cd-results-sort" className="cd-label">{sortLabel(tab)}</label>
                    <select
                      id="cd-results-sort"
                      className="cd-select cd-select--sm"
                      value={filters.sort}
                      onChange={(e) => handleFilterChange('sort', e.target.value)}
                    >
                      <option value="newest">Newest first</option>
                      <option value="oldest">Oldest first</option>
                    </select>
                  </div>
                </div>
              </div>

              {/* Declaration list — each row is its own card */}
              {listLoading ? (
                <ul className="cd-list" aria-busy="true" aria-label="Loading declarations">
                  {Array.from({ length: PAGE_SIZE }, (_, k) => (
                    <li key={k} className="cd-list-item"><SkeletonCard /></li>
                  ))}
                </ul>
              ) : listError && !isGateError(listError?.code) ? (
                <div className="cd-error-state">
                  <EmptyState
                    icon={AlertCircle}
                    title="Couldn't load declarations"
                    message="Something went wrong while fetching declarations."
                    action={
                      <Button variant="secondary" icon={RefreshCw} onClick={refetchList}>
                        Retry
                      </Button>
                    }
                  />
                </div>
              ) : items.length === 0 ? (
                <div className="cd-empty-state">
                  <EmptyState
                    icon={FileText}
                    title={hasFilters ? 'No declarations match these filters' : `No ${tab} declarations`}
                    message={hasFilters ? undefined : `There are no ${tab} declarations for this subscription.`}
                    action={hasFilters ? (
                      <Button variant="secondary" icon={RefreshCw} onClick={handleClearFilters}>
                        Clear filters
                      </Button>
                    ) : undefined}
                  />
                </div>
              ) : (
                <ul className="cd-list" role="list" aria-label="Declarations">
                  {items.map((item) => (
                    <li key={item.cusDecId} className="cd-list-item">
                      <CusDecCard
                        item={item}
                        subscriptionType={strip.subscriptionType}
                        containersBlocked={strip.containersBlocked}
                        busyCardId={busyCardId}
                        highlighted={highlightedCardId === item.cusDecId}
                        onAcknowledge={handleAcknowledge}
                        onDisclaimRequest={setDisclaimTarget}
                        onComplete={handleComplete}
                        onReopen={handleReopen}
                      />
                    </li>
                  ))}
                </ul>
              )}

              {/* Pagination — own slim rounded block */}
              {totalCount > PAGE_SIZE && (
                <div className="cd-pagination-block">
                  <DotPagination
                    page={page}
                    totalCount={totalCount}
                    pageSize={PAGE_SIZE}
                    onPageChange={setPage}
                  />
                </div>
              )}
            </div>
          </>
        )}
      </div>

      {/* DisclaimDialog always rendered; Modal handles open/close animation */}
      <DisclaimDialog
        item={disclaimTarget}
        busy={disclaimBusy}
        onConfirm={handleDisclaimConfirm}
        onCancel={() => { if (!disclaimBusy) setDisclaimTarget(null); }}
      />
    </PageTransition>
  );
}
