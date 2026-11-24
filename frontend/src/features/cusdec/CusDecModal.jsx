import { Fragment, useEffect, useRef, useState } from 'react';
import {
  Activity, AlertCircle, ArrowRight, Building2, ChevronDown,
  Clipboard, EyeOff, FileText, Loader2, Package, RefreshCw,
} from 'lucide-react';
import { motion, useReducedMotion } from 'motion/react';
import Modal from '../../components/ui/Modal';
import { Button, Pill } from '../../components/ui';
import { formatDate, formatDatetime, formatDatetimeCompact } from './formatHelpers';
import { COMPLETE_REQUIRES_EXIT } from './features';
import { getCusDecDetail, getCusDecTimeline } from './cusdecApi';

const MODAL_TABS = [
  { key: 'details',  label: 'Details' },
  { key: 'tracking', label: 'Tracking' },
];

const _moneyFmt = new Intl.NumberFormat('en-US', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

function formatMoney(val) {
  if (val == null) return '—';
  const n = Number(val);
  return isNaN(n) ? '—' : _moneyFmt.format(n);
}

const _countryNames = (() => {
  try { return new Intl.DisplayNames(['en'], { type: 'region' }); } catch { return null; }
})();

function fmtCountry(code) {
  if (!code) return null;
  const upper = code.toUpperCase();
  if (upper.length !== 2 || !_countryNames) return code;
  try { return _countryNames.of(upper) ?? code; } catch { return code; }
}

function channelTone(ch) {
  switch (ch?.toLowerCase()) {
    case 'green':  return 'green';
    case 'yellow': return 'amber';
    case 'red':    return 'red';
    case 'blue':   return 'blue';
    default:       return 'grey';
  }
}

function StatusPill({ item }) {
  if (!item) return null;
  const { tab, state, currentStatus } = item;
  if (tab === 'disclaimed') return <Pill tone="red">Disclaimed</Pill>;
  if (tab === 'completed') {
    const tone = currentStatus?.toLowerCase().includes('cancel') ? 'grey' : 'green';
    return <Pill tone={tone}>{currentStatus || 'Completed'}</Pill>;
  }
  if (state === 'new') return <Pill tone="amber">New</Pill>;
  return <Pill tone="blue">{currentStatus || 'In progress'}</Pill>;
}

export default function CusDecModal({
  item,
  initialTab,
  subscriptionType,
  subId,
  busyId,
  onAcknowledge,
  onComplete,
  onClose,
}) {
  const [lastItem, setLastItem] = useState(null);
  useEffect(() => { if (item) setLastItem(item); }, [item]);
  const displayed = item ?? lastItem;

  const [activeTab, setActiveTab] = useState(initialTab ?? 'details');
  useEffect(() => {
    if (!item) return;
    // If disclaimed, details only
    const tab = item.tab === 'disclaimed' ? 'details' : (initialTab ?? 'details');
    setActiveTab(tab);
  }, [item, initialTab]);

  const shouldReduce = useReducedMotion();

  const [detailState, setDetailState] = useState({ status: 'idle', data: null });
  const [timelineState, setTimelineState] = useState({ status: 'idle', data: null });
  const [justAcknowledged, setJustAcknowledged] = useState(false);
  const ctrlsRef = useRef({ detail: null, timeline: null });
  const prevItemStateRef = useRef(undefined);

  // Abort in-flight requests and reset data when the row changes (or modal closes)
  useEffect(() => {
    ctrlsRef.current.detail?.abort();
    ctrlsRef.current.timeline?.abort();
    ctrlsRef.current = { detail: null, timeline: null };
    setDetailState({ status: 'idle', data: null });
    setTimelineState({ status: 'idle', data: null });
    setJustAcknowledged(false);
    prevItemStateRef.current = item?.state;
  }, [item?.cusDecId]); // eslint-disable-line react-hooks/exhaustive-deps

  // Detect new → inprogress transition within the same item
  useEffect(() => {
    if (!item) { prevItemStateRef.current = undefined; return; }
    const prev = prevItemStateRef.current;
    const curr = item.state;
    prevItemStateRef.current = curr;
    if (prev === 'new' && curr !== 'new') {
      setJustAcknowledged(true);
      setTimelineState({ status: 'idle', data: null });
    }
  }, [item?.state]); // eslint-disable-line react-hooks/exhaustive-deps

  // Fetch detail whenever status is idle and the Details tab is active
  useEffect(() => {
    if (activeTab !== 'details') return;
    if (!item) return;
    if (!displayed?.cusDecId || !subId) return;
    if (detailState.status !== 'idle') return;

    const ctrl = new AbortController();
    ctrlsRef.current.detail?.abort();
    ctrlsRef.current.detail = ctrl;
    setDetailState({ status: 'loading', data: null });

    getCusDecDetail(subId, displayed.cusDecId)
      .then((data) => {
        if (ctrl.signal.aborted) return;
        setDetailState({ status: 'success', data });
      })
      .catch((err) => {
        if (ctrl.signal.aborted) return;
        const isNotAvailable = err.status === 404 || err.status === 403;
        setDetailState({ status: isNotAvailable ? 'notfound' : 'error', data: null });
      });
  }, [activeTab, item, displayed?.cusDecId, subId, detailState.status]); // eslint-disable-line react-hooks/exhaustive-deps

  // Fetch timeline whenever status is idle, Tracking tab is active, and item is not new
  useEffect(() => {
    if (activeTab !== 'tracking') return;
    if (!item) return;
    if (item.state === 'new') return;
    if (!displayed?.cusDecId || !subId) return;
    if (timelineState.status !== 'idle') return;

    const ctrl = new AbortController();
    ctrlsRef.current.timeline?.abort();
    ctrlsRef.current.timeline = ctrl;
    setTimelineState({ status: 'loading', data: null });

    getCusDecTimeline(subId, displayed.cusDecId)
      .then((data) => {
        if (ctrl.signal.aborted) return;
        setTimelineState({ status: 'success', data });
      })
      .catch((err) => {
        if (ctrl.signal.aborted) return;
        const isNotAvailable = err.status === 404 || err.status === 403;
        setTimelineState({ status: isNotAvailable ? 'notfound' : 'error', data: null });
      });
  }, [activeTab, item, displayed?.cusDecId, subId, timelineState.status]); // eslint-disable-line react-hooks/exhaustive-deps

  // Tabs visible to user
  const visibleTabs = displayed?.tab === 'disclaimed'
    ? MODAL_TABS.filter((t) => t.key === 'details')
    : MODAL_TABS;

  const isBusy = busyId === displayed?.cusDecId;

  // Complete button: use timeline's isExited/state, not the list row
  const timelineData = timelineState.status === 'success' ? timelineState.data : null;
  const showCompleteBtn =
    activeTab === 'tracking' &&
    timelineData?.isExited === true &&
    timelineData?.state === 'inprogress' &&
    (!COMPLETE_REQUIRES_EXIT || timelineData?.isExited);

  let footer;
  if (showCompleteBtn) {
    footer = (
      <Button
        variant="primary"
        style={{ width: '100%' }}
        disabled={isBusy}
        aria-busy={isBusy}
        onClick={() => onComplete(displayed.cusDecId)}
      >
        {isBusy
          ? <><Loader2 size={16} className="cd-spin" aria-hidden="true" /> Completing CusDec…</>
          : 'Complete CusDec'}
      </Button>
    );
  } else if (
    activeTab === 'tracking' &&
    timelineData &&
    displayed?.tab === 'completed'
  ) {
    const datePart = displayed.completedAt ? formatDate(displayed.completedAt) : '—';
    const byPart = displayed.completedByName ? ` by ${displayed.completedByName}` : '';
    footer = (
      <p className="cdm-completed-note">Completed {datePart}{byPart}</p>
    );
  }

  function handleTabKeyDown(e, currentKey) {
    const keys = visibleTabs.map((t) => t.key);
    const idx = keys.indexOf(currentKey);
    let newIdx = -1;
    if (e.key === 'ArrowRight') newIdx = (idx + 1) % keys.length;
    else if (e.key === 'ArrowLeft') newIdx = (idx - 1 + keys.length) % keys.length;
    else if (e.key === 'Home') newIdx = 0;
    else if (e.key === 'End') newIdx = keys.length - 1;
    else if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); setActiveTab(currentKey); return; }
    else return;
    e.preventDefault();
    document.getElementById(`cdm-tab-${keys[newIdx]}`)?.focus();
  }

  const cpLabel = subscriptionType === 'Declarant' ? 'Consignee' : 'Declarant';
  const cpName = subscriptionType === 'Declarant'
    ? displayed?.consigneeName || '—'
    : displayed?.declarantName || '—';

  const tabBar = displayed ? (
    <div className="cdm-tabs" role="tablist" aria-label="CusDec sections">
      {visibleTabs.map(({ key, label }) => {
        const isActive = activeTab === key;
        return (
          <button
            key={key}
            type="button"
            role="tab"
            id={`cdm-tab-${key}`}
            aria-selected={isActive}
            aria-controls={`cdm-panel-${key}`}
            tabIndex={isActive ? 0 : -1}
            className={`cdm-tab${isActive ? ' cdm-tab--active' : ''}`}
            onClick={() => setActiveTab(key)}
            onKeyDown={(e) => handleTabKeyDown(e, key)}
          >
            {isActive && !shouldReduce && (
              <motion.span
                layoutId="cdm-tab-bg"
                className="cdm-tab__bg"
                aria-hidden="true"
                transition={{ type: 'spring', stiffness: 500, damping: 50 }}
              />
            )}
            <span className="cdm-tab__text">{label}</span>
          </button>
        );
      })}
    </div>
  ) : null;

  return (
    <Modal
      open={!!item}
      onClose={onClose}
      title={displayed?.cusDecNumber ?? '—'}
      subtitle={displayed ? `${cpLabel}: ${cpName}${displayed.office ? ` · ${displayed.office}` : ''}` : undefined}
      size="lg"
      fixedHeight
      tabs={tabBar}
      footer={footer}
    >
      {displayed && (
        <div
          role="tabpanel"
          id={`cdm-panel-${activeTab}`}
          aria-labelledby={`cdm-tab-${activeTab}`}
        >
          {activeTab === 'details' && (
            <DetailsTab
              item={displayed}
              state={detailState}
              onRetry={() => setDetailState({ status: 'idle', data: null })}
            />
          )}
          {activeTab === 'tracking' && (
            displayed.state === 'new' ? (
              <TrackingLockedPreview
                cusDecId={displayed.cusDecId}
                isBusy={isBusy}
                onAcknowledge={onAcknowledge}
              />
            ) : (
              <div className={justAcknowledged && !shouldReduce ? 'cdm-tracking-fade-in' : undefined}>
                <TrackingTab
                  item={displayed}
                  state={timelineState}
                  onRetry={() => setTimelineState({ status: 'idle', data: null })}
                />
              </div>
            )
          )}
        </div>
      )}
    </Modal>
  );
}

// ── Card shell ────────────────────────────────────────────────────────────────

function CardShell({ icon, title, meta, animIndex, children }) {
  const shouldReduce = useReducedMotion();
  return (
    <motion.section
      className="cdm-card"
      initial={shouldReduce ? false : { opacity: 0, y: 8 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.22, ease: 'easeOut', delay: shouldReduce ? 0 : animIndex * 0.07 }}
    >
      <div className="cdm-card__head">
        <span className="cdm-card__icon" aria-hidden="true">{icon}</span>
        <h3 className="cdm-card__title">{title}</h3>
        {meta && <div className="cdm-card__meta">{meta}</div>}
      </div>
      <div className="cdm-card__body">{children}</div>
    </motion.section>
  );
}

// ── Details tab ───────────────────────────────────────────────────────────────

function DetailsTab({ item, state, onRetry }) {
  if (state.status === 'loading' || state.status === 'idle') {
    return <DetailsSkeleton />;
  }

  if (state.status === 'notfound') {
    return (
      <div className="cdm-state-block">
        <p className="cdm-state-block__text">Detail information is not available for this declaration.</p>
      </div>
    );
  }

  if (state.status === 'error') {
    return (
      <div className="cdm-state-block cdm-state-block--error">
        <AlertCircle size={20} aria-hidden="true" className="cdm-state-block__icon" />
        <p className="cdm-state-block__text">Couldn't load declaration details.</p>
        <Button variant="secondary" icon={RefreshCw} onClick={onRetry}>Retry</Button>
      </div>
    );
  }

  const d = state.data;

  return (
    <div className="cdm-detail">
      <PartiesCard d={d} />
      <StatusShipmentCard d={d} item={item} />
      <ItemsCard d={d} />
      {d.inspections?.length > 0 && <InspectionsCard d={d} />}
    </div>
  );
}

// ── 1. Parties ────────────────────────────────────────────────────────────────

function PartiesCard({ d }) {
  const exporterName = d.exporter || null;
  const supplierName = d.supplierCompanyName || null;
  const namesEqual = !!(exporterName && supplierName && exporterName === supplierName);
  const hasSecondary = !!(exporterName || supplierName);

  return (
    <CardShell icon={<Building2 size={15} />} title="Parties" animIndex={0}>
      <div className="cdm-parties-row">
        <PartyBlock label="Declarant" name={d.declarantCompanyName} tin={d.declarantTin} />
        <ArrowRight size={16} className="cdm-parties-arrow" aria-hidden="true" />
        <PartyBlock label="Consignee" name={d.consigneeCompanyName} tin={d.consigneeTin} />
      </div>
      {hasSecondary && (
        <div className="cdm-parties-secondary">
          {namesEqual ? (
            <span>
              <span className="cdm-parties-secondary__label">Exporter / Supplier</span>
              {' '}{exporterName}
            </span>
          ) : (
            <>
              {exporterName && (
                <span>
                  <span className="cdm-parties-secondary__label">Exporter</span>
                  {' '}{exporterName}
                </span>
              )}
              {supplierName && (
                <span>
                  <span className="cdm-parties-secondary__label">Supplier</span>
                  {' '}{supplierName}
                </span>
              )}
            </>
          )}
        </div>
      )}
    </CardShell>
  );
}

function PartyBlock({ label, name, tin }) {
  return (
    <div className="cdm-party-block">
      <span className="cdm-party-block__label">{label}</span>
      <span className="cdm-party-block__name">{name || '—'}</span>
      {tin && <span className="cdm-party-block__tin">TIN {tin}</span>}
    </div>
  );
}

// ── 2. Status & Shipment ──────────────────────────────────────────────────────

function StatusShipmentCard({ d, item }) {
  const originName  = fmtCountry(d.origin);
  const destName    = fmtCountry(d.destination);
  const route = originName && destName ? (
    <>
      <span title={d.origin || undefined}>{originName}</span>
      {' → '}
      <span title={d.destination || undefined}>{destName}</span>
    </>
  ) : (originName || destName || '—');

  const fields = [
    ['Office',        d.officeCode || '—'],
    ['Type',          d.cusDecType || '—'],
    ['Incoterm',      d.incoterm || '—'],
    ['Route',         route],
    ['BL number',     d.blNumber || '—'],
    ['Invoice value', formatMoney(d.invoiceValue)],
    ['Containers',    d.noOfContainers ?? '—'],
  ];

  const tone = channelTone(d.channel);

  return (
    <CardShell icon={<Activity size={15} />} title="Status & Shipment" animIndex={1}>
      <div className="cdm-stat-tiles">
        <div className="cdm-stat-tile">
          <span className="cdm-stat-tile__label">Status</span>
          <div className="cdm-stat-tile__value">
            <div className="cdm-stat-tile__pills">
              <StatusPill item={item} />
              {d.isExited && item.tab === 'tracking' && item.state === 'inprogress' && (
                <Pill tone="green">Exited</Pill>
              )}
            </div>
            {d.lastUpdate && (
              <span className="cdm-stat-tile__sub">Last update {formatDatetimeCompact(d.lastUpdate)}</span>
            )}
          </div>
        </div>
        <div className="cdm-stat-tile">
          <span className="cdm-stat-tile__label">Channel</span>
          <div className="cdm-stat-tile__value">
            {d.channel
              ? <span className={`cdm-channel-chip cdm-channel-chip--${tone}`}>{d.channel}</span>
              : <span className="cdm-stat-tile__empty">—</span>}
          </div>
        </div>
        <div className="cdm-stat-tile">
          <span className="cdm-stat-tile__label">Registered</span>
          <div className="cdm-stat-tile__value">{formatDate(d.registrationDate)}</div>
        </div>
        {d.releaseDate && (
          <div className="cdm-stat-tile">
            <span className="cdm-stat-tile__label">Released</span>
            <div className="cdm-stat-tile__value">{formatDate(d.releaseDate)}</div>
          </div>
        )}
      </div>

      <dl className="cdm-field-grid">
        {fields.map(([label, value]) => (
          <Fragment key={label}>
            <dt className="cdm-field-grid__label">{label}</dt>
            <dd className="cdm-field-grid__value">{value}</dd>
          </Fragment>
        ))}
      </dl>
    </CardShell>
  );
}

// ── 3. Items ──────────────────────────────────────────────────────────────────

function TaxPanel({ taxLines, taxTotal }) {
  return (
    <div className="cdm-tax-detail">
      <table className="cdm-tax-table">
        <thead>
          <tr>
            <th className="cdm-tax-table__th">Tax code</th>
            <th className="cdm-tax-table__th cdm-tax-table__th--num">Rank</th>
            <th className="cdm-tax-table__th cdm-tax-table__th--num">Amount</th>
          </tr>
        </thead>
        <tbody>
          {[...taxLines].sort((a, b) => a.taxRank - b.taxRank).map((tl, ti) => (
            <tr key={ti}>
              <td className="cdm-tax-table__td">{tl.taxCode}</td>
              <td className="cdm-tax-table__td cdm-tax-table__td--num">{tl.taxRank}</td>
              <td className="cdm-tax-table__td cdm-tax-table__td--num">{formatMoney(tl.taxAmount)}</td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr className="cdm-tax-table__subtotal">
            <td className="cdm-tax-table__td">Subtotal</td>
            <td className="cdm-tax-table__td cdm-tax-table__td--num" />
            <td className="cdm-tax-table__td cdm-tax-table__td--num">{formatMoney(taxTotal)}</td>
          </tr>
        </tfoot>
      </table>
    </div>
  );
}

function ItemsCard({ d }) {
  const [expanded, setExpanded] = useState({});
  const items = d.items ?? [];
  const taxByItem = {};
  for (const tg of (d.taxes ?? [])) taxByItem[tg.itemNumber] = tg.taxes ?? [];

  const totalValue = items.reduce((s, it) => s + (Number(it.totalValue) || 0), 0);
  const totalTaxes = (d.taxes ?? [])
    .flatMap((tg) => tg.taxes ?? [])
    .reduce((s, t) => s + (Number(t.taxAmount) || 0), 0);

  // Totals live in the table footer; meta shows truncation note only
  const meta = d.itemsTruncated
    ? <span className="cdm-card__meta-note">showing {items.length} of {d.noOfItems}</span>
    : null;

  function toggle(num) {
    setExpanded((prev) => ({ ...prev, [num]: !prev[num] }));
  }

  return (
    <CardShell
      icon={<Package size={15} />}
      title={`Items (${d.noOfItems ?? items.length})`}
      meta={meta}
      animIndex={2}
    >
      {items.length === 0 ? (
        <div className="cdm-state-block cdm-state-block--inline">
          <p className="cdm-state-block__text">No items recorded.</p>
        </div>
      ) : (
        <>
          {/* ── Desktop table ─────────────────────────────────────────────── */}
          <div className="cdm-items-wrap">
            <table className="cdm-items-table">
              <thead>
                <tr>
                  <th className="cdm-items-table__th cdm-items-table__th--no">#</th>
                  <th className="cdm-items-table__th cdm-items-table__th--item">Item</th>
                  <th className="cdm-items-table__th cdm-items-table__th--num">Qty</th>
                  <th className="cdm-items-table__th cdm-items-table__th--num">Unit price</th>
                  <th className="cdm-items-table__th cdm-items-table__th--num">Value</th>
                  <th className="cdm-items-table__th cdm-items-table__th--num">Taxes</th>
                </tr>
              </thead>
              <tbody>
                {items.map((it) => {
                  const taxLines = taxByItem[it.itemNumber] ?? [];
                  const taxTotal = taxLines.reduce((s, t) => s + (Number(t.taxAmount) || 0), 0);
                  const isExp = !!expanded[it.itemNumber];
                  const hasTaxes = taxLines.length > 0;
                  const panelId = `cdm-tax-panel-${it.itemNumber}`;
                  return (
                    <Fragment key={it.itemNumber}>
                      <tr
                        className={[
                          'cdm-items-table__row',
                          hasTaxes ? 'cdm-items-table__row--expandable' : '',
                          isExp    ? 'cdm-items-table__row--open' : '',
                        ].filter(Boolean).join(' ')}
                        {...(hasTaxes ? { onClick: () => toggle(it.itemNumber) } : {})}
                      >
                        <td className="cdm-items-table__td cdm-items-table__td--no">{it.itemNumber}</td>
                        <td className="cdm-items-table__td cdm-items-table__td--item">
                          <div className="cdm-items-table__item-cell">
                            <div className="cdm-items-table__item-text">
                              <span className="cdm-items-table__desc">{it.description || '—'}</span>
                              {it.hsCode && <span className="cdm-items-table__hs">{it.hsCode}</span>}
                            </div>
                            {hasTaxes && (
                              <button
                                type="button"
                                aria-label={`${isExp ? 'Collapse' : 'Expand'} tax lines for item ${it.itemNumber}`}
                                aria-expanded={isExp}
                                aria-controls={panelId}
                                className={`cdm-items-expand-btn${isExp ? ' cdm-items-expand-btn--open' : ''}`}
                                onClick={(e) => { e.stopPropagation(); toggle(it.itemNumber); }}
                              >
                                <ChevronDown size={12} aria-hidden="true" />
                              </button>
                            )}
                          </div>
                        </td>
                        <td className="cdm-items-table__td cdm-items-table__td--num">{it.quantity ?? '—'}</td>
                        <td className="cdm-items-table__td cdm-items-table__td--num">{formatMoney(it.unitPrice)}</td>
                        <td className="cdm-items-table__td cdm-items-table__td--num">{formatMoney(it.totalValue)}</td>
                        <td className="cdm-items-table__td cdm-items-table__td--num">
                          {hasTaxes ? formatMoney(taxTotal) : '—'}
                        </td>
                      </tr>
                      {isExp && hasTaxes && (
                        <tr className="cdm-items-table__taxrow">
                          <td colSpan={6}>
                            <div id={panelId}>
                              <TaxPanel taxLines={taxLines} taxTotal={taxTotal} />
                            </div>
                          </td>
                        </tr>
                      )}
                    </Fragment>
                  );
                })}
              </tbody>
              <tfoot>
                <tr className="cdm-items-table__totals">
                  <td colSpan={4} className="cdm-items-table__totals-label">Totals</td>
                  <td className="cdm-items-table__td--num cdm-items-table__totals-val">{formatMoney(totalValue)}</td>
                  <td className="cdm-items-table__td--num cdm-items-table__totals-val">{formatMoney(totalTaxes)}</td>
                </tr>
              </tfoot>
            </table>
          </div>

          {/* ── Mobile cards ───────────────────────────────────────────────── */}
          <div className="cdm-items-cards">
            {items.map((it) => {
              const taxLines = taxByItem[it.itemNumber] ?? [];
              const taxTotal = taxLines.reduce((s, t) => s + (Number(t.taxAmount) || 0), 0);
              const isExp = !!expanded[it.itemNumber];
              const hasTaxes = taxLines.length > 0;
              return (
                <div key={it.itemNumber} className="cdm-item-card">
                  <div className="cdm-item-card__header">
                    <span className="cdm-item-card__no">Item {it.itemNumber}</span>
                    {it.hsCode && <span className="cdm-item-card__hs">{it.hsCode}</span>}
                  </div>
                  <p className="cdm-item-card__desc">{it.description || '—'}</p>
                  <div className="cdm-item-card__row">
                    <span className="cdm-item-card__label">Qty</span>
                    <span>{it.quantity ?? '—'}</span>
                    <span className="cdm-item-card__label">Unit price</span>
                    <span>{formatMoney(it.unitPrice)}</span>
                    <span className="cdm-item-card__label">Value</span>
                    <span>{formatMoney(it.totalValue)}</span>
                  </div>
                  {hasTaxes && (
                    <>
                      <button
                        type="button"
                        className={`cdm-expand-btn cdm-expand-btn--card${isExp ? ' cdm-expand-btn--open' : ''}`}
                        aria-expanded={isExp}
                        onClick={() => toggle(it.itemNumber)}
                      >
                        <ChevronDown size={12} aria-hidden="true" />
                        {isExp ? 'Hide' : 'Show'} {taxLines.length} tax line{taxLines.length !== 1 ? 's' : ''}
                      </button>
                      {isExp && <TaxPanel taxLines={taxLines} taxTotal={taxTotal} />}
                    </>
                  )}
                </div>
              );
            })}
          </div>
        </>
      )}
    </CardShell>
  );
}

// ── 4. Inspections ────────────────────────────────────────────────────────────

function InspectionsCard({ d }) {
  const [openIdx, setOpenIdx] = useState(-1);
  const inspections = d.inspections ?? [];

  function toggle(i) {
    setOpenIdx((prev) => (prev === i ? -1 : i));
  }

  function handleKeyDown(e, i) {
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      document.getElementById(`cdm-ins-${i + 1}-hd`)?.focus();
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      document.getElementById(`cdm-ins-${i - 1}-hd`)?.focus();
    }
  }

  return (
    <CardShell
      icon={<Clipboard size={15} />}
      title={`Inspections (${inspections.length})`}
      animIndex={3}
    >
      <div className="cdm-accord">
        {inspections.map((ins, i) => {
          const isOpen = openIdx === i;
          const sections = [...(ins.sections ?? [])].sort((a, b) => a.keySecNumber - b.keySecNumber);
          const headingId = `cdm-ins-${i}-hd`;
          const panelId   = `cdm-ins-${i}-pn`;

          return (
            <div key={i} className={`cdm-accord-item${isOpen ? ' cdm-accord-item--open' : ''}`}>
              <button
                type="button"
                id={headingId}
                className="cdm-accord-trigger"
                aria-expanded={isOpen}
                aria-controls={panelId}
                onClick={() => toggle(i)}
                onKeyDown={(e) => handleKeyDown(e, i)}
              >
                <span className="cdm-accord-trigger__code">{ins.issueCode}</span>
                {ins.description && (
                  <span className="cdm-accord-trigger__desc">{ins.description}</span>
                )}
                {sections.length > 0 && (
                  <span className="cdm-accord-trigger__meta">
                    {sections.length} section{sections.length !== 1 ? 's' : ''}
                  </span>
                )}
                <ChevronDown size={14} className="cdm-accord-trigger__chevron" aria-hidden="true" />
              </button>
              {isOpen && (
                <div id={panelId} role="region" aria-labelledby={headingId} className="cdm-accord-body">
                  {ins.value && (
                    <p className="cdm-accord-body__row">
                      <span className="cdm-accord-body__key">Value</span>
                      {formatMoney(ins.value)}
                    </p>
                  )}
                  {ins.frdNfr && (
                    <p className="cdm-accord-body__row">
                      <span className="cdm-accord-body__key">FRD ref</span>
                      {ins.frdNfr}
                    </p>
                  )}
                  {sections.length > 0 && (
                    <ul className="cdm-accord-body__sections">
                      {sections.map((sec) => (
                        <li key={sec.keySecNumber} className="cdm-accord-body__section">
                          <span className="cdm-accord-body__sec-key">{sec.keySecNumber}</span>
                          <span className="cdm-accord-body__sec-name">{sec.sectionName}</span>
                          {sec.sectionDate && (
                            <span className="cdm-accord-body__sec-date">{formatDatetime(sec.sectionDate)}</span>
                          )}
                        </li>
                      ))}
                    </ul>
                  )}
                </div>
              )}
            </div>
          );
        })}
      </div>
    </CardShell>
  );
}

// ── Detail skeleton ───────────────────────────────────────────────────────────

function DetailsSkeleton() {
  return (
    <div className="cdm-detail" aria-busy="true" aria-label="Loading details">
      {[70, 140, 90].map((w, i) => (
        <div key={i} className="cdm-card">
          <div className="cdm-card__head">
            <div className="cdm-skel cdm-skel--icon-sq" />
            <div className="cdm-skel cdm-skel--card-title" style={{ width: w }} />
          </div>
          <div className="cdm-card__body">
            <div className="cdm-skel cdm-skel--card-body" />
          </div>
        </div>
      ))}
    </div>
  );
}

// ── Tracking tab ──────────────────────────────────────────────────────────────

function TrackingTab({ item, state, onRetry }) {
  if (state.status === 'loading' || state.status === 'idle') {
    return <TrackingSkeleton />;
  }

  if (state.status === 'notfound') {
    return (
      <div className="cdm-state-block">
        <p className="cdm-state-block__text">Tracking timeline is not available for this declaration.</p>
      </div>
    );
  }

  if (state.status === 'error') {
    return (
      <div className="cdm-state-block cdm-state-block--error">
        <AlertCircle size={20} aria-hidden="true" className="cdm-state-block__icon" />
        <p className="cdm-state-block__text">Couldn't load the tracking timeline.</p>
        <Button variant="secondary" icon={RefreshCw} onClick={onRetry}>Retry</Button>
      </div>
    );
  }

  const d = state.data;
  const entries = d.items ?? [];

  return (
    <div className="cdm-tracking-tab">
      {/* Header */}
      <div className="cdm-tracking-header">
        <div className="cdm-tracking-header__status">
          <StatusPill item={item} />
          {d.isExited && item.tab === 'tracking' && item.state === 'inprogress' && (
            <Pill tone="green">Exited</Pill>
          )}
        </div>
        {d.lastUpdate && (
          <p className="cdm-tracking-header__update">
            Last update {formatDatetime(d.lastUpdate)}
          </p>
        )}
        {d.truncated && (
          <p className="cdm-tracking-header__truncated">
            Showing latest entries only — older history not shown.
          </p>
        )}
      </div>

      {/* Timeline */}
      {entries.length === 0 ? (
        <div className="cdm-state-block">
          <p className="cdm-state-block__text">No timeline entries yet.</p>
        </div>
      ) : (
        <ol className="cdm-timeline" aria-label="Declaration timeline">
          {entries.map((entry, i) => {
            const isFirst = i === 0;
            const isInspection = entry.kind === 'inspection';
            return (
              <li
                key={i}
                className={[
                  'cdm-timeline__item',
                  isFirst ? 'cdm-timeline__item--current' : '',
                  isInspection ? 'cdm-timeline__item--inspection' : '',
                ].filter(Boolean).join(' ')}
              >
                <span className="cdm-timeline__icon" aria-hidden="true">
                  {isInspection
                    ? <Clipboard size={14} />
                    : <FileText size={14} />}
                </span>
                <div className="cdm-timeline__body">
                  <span className="cdm-timeline__label">
                    {entry.label}
                    {isInspection && (
                      <span className="cdm-timeline__tag">Inspection</span>
                    )}
                  </span>
                  {entry.dateUtc && (
                    <span className="cdm-timeline__date">{formatDatetime(entry.dateUtc)}</span>
                  )}
                </div>
              </li>
            );
          })}
        </ol>
      )}
    </div>
  );
}

// ── Tracking locked preview (new state — before acknowledge) ─────────────────

function TrackingLockedPreview({ cusDecId, isBusy, onAcknowledge }) {
  return (
    <div className="cdm-tracking-locked">
      {/* Neutral skeleton shapes — blurred, fully non-interactive */}
      <div className="cdm-tracking-locked__blur" aria-hidden="true" inert="">
        <TrackingSkeleton />
      </div>
      {/* Centred overlay */}
      <div className="cdm-tracking-locked__overlay">
        <div className="cdm-tracking-locked__card">
          <EyeOff size={20} aria-hidden="true" className="cdm-tracking-locked__icon" />
          <h3 id="cdm-tl-heading" className="cdm-tracking-locked__heading">
            Acknowledge to start tracking
          </h3>
          <p id="cdm-tl-desc" className="cdm-tracking-locked__body">
            Confirm this declaration belongs to your company to see its status history.
          </p>
          <Button
            variant="primary"
            style={{ width: '100%' }}
            disabled={isBusy}
            aria-busy={isBusy}
            aria-describedby="cdm-tl-desc"
            onClick={() => onAcknowledge(cusDecId)}
          >
            {isBusy
              ? <><Loader2 size={16} className="cd-spin" aria-hidden="true" /> Acknowledging…</>
              : 'Acknowledge'}
          </Button>
        </div>
      </div>
    </div>
  );
}

function TrackingSkeleton() {
  return (
    <div className="cdm-tracking-tab cdm-tracking-tab--loading" aria-busy="true" aria-label="Loading timeline">
      <div className="cdm-tracking-header">
        <div className="cdm-skel cdm-skel--pill" />
        <div className="cdm-skel cdm-skel--label" style={{ marginTop: 8 }} />
      </div>
      <ol className="cdm-timeline">
        {Array.from({ length: 4 }, (_, i) => (
          <li key={i} className="cdm-timeline__item">
            <span className="cdm-timeline__icon cdm-timeline__icon--skel" aria-hidden="true" />
            <div className="cdm-timeline__body">
              <div className="cdm-skel cdm-skel--timeline-label" />
              <div className="cdm-skel cdm-skel--timeline-date" />
            </div>
          </li>
        ))}
      </ol>
    </div>
  );
}
