import { useState } from 'react';
import { X, SlidersHorizontal, ChevronDown } from 'lucide-react';
import { AnimatePresence, motion, useReducedMotion } from 'motion/react';
import { useMediaQuery } from '../../lib/useMediaQuery';

function dateLabel(tab) {
  if (tab === 'completed') return 'Completed';
  if (tab === 'disclaimed') return 'Disclaimed';
  return 'Registered';
}

function counterpartyLabel(subscriptionType) {
  return subscriptionType === 'Declarant' ? 'Consignee TIN' : 'Declarant TIN';
}

export default function FilterBar({
  tab,
  subscriptionType,
  offices,
  filters,
  onChange,
  onClear,
}) {
  const [panelOpen, setPanelOpen] = useState(false);
  const shouldReduce = useReducedMotion();
  const isMobile = useMediaQuery('(max-width: 700px)');

  const { search, cusDecNumber, counterpartyTin, office, fromDate, toDate } = filters;

  const rangeLabel = dateLabel(tab);
  const tinLabel = counterpartyLabel(subscriptionType);

  const advancedCount = [cusDecNumber, counterpartyTin].filter(Boolean).length;
  const mobileFilterCount = [office, fromDate, toDate, cusDecNumber, counterpartyTin].filter(Boolean).length;
  const hasAny = !!(search || cusDecNumber || counterpartyTin || office || fromDate || toDate);

  const chips = [];
  if (search) chips.push({ key: 'search', label: `"${search}"`, clear: () => onChange('search', '') });
  if (cusDecNumber) chips.push({ key: 'cusDecNumber', label: `CusDec: "${cusDecNumber}"`, clear: () => onChange('cusDecNumber', '') });
  if (counterpartyTin) chips.push({ key: 'counterpartyTin', label: `${tinLabel}: "${counterpartyTin}"`, clear: () => onChange('counterpartyTin', '') });
  if (office) chips.push({ key: 'office', label: `Office: ${office}`, clear: () => onChange('office', '') });
  if (fromDate || toDate) chips.push({
    key: 'dates',
    label: `${rangeLabel}: ${fromDate || '…'} → ${toDate || '…'}`,
    clear: () => { onChange('fromDate', ''); onChange('toDate', ''); },
  });

  const panelMotion = shouldReduce
    ? { initial: { opacity: 1 }, animate: { opacity: 1 }, exit: { opacity: 0 }, transition: { duration: 0.08 } }
    : { initial: { opacity: 0, height: 0, overflow: 'hidden' }, animate: { opacity: 1, height: 'auto', overflow: 'hidden' }, exit: { opacity: 0, height: 0, overflow: 'hidden' }, transition: { duration: 0.18, ease: 'easeOut' } };

  const chipsMotion = shouldReduce
    ? { initial: { opacity: 1 }, animate: { opacity: 1 }, exit: { opacity: 0 }, transition: { duration: 0.08 } }
    : { initial: { opacity: 0, height: 0, overflow: 'hidden' }, animate: { opacity: 1, height: 'auto', overflow: 'hidden' }, exit: { opacity: 0, height: 0, overflow: 'hidden' }, transition: { duration: 0.15, ease: 'easeOut' } };

  return (
    <div className="cd-filterbar" role="search" aria-label="Filter declarations">

      {/* ── Desktop row: 3 blocks + More filters + Clear ──────────────────── */}
      {!isMobile && (
        <div className="cd-filterbar-row">
          {/* Block 1 — Search */}
          <div className="cd-filter-block cd-filter-block--search">
            <label htmlFor="cd-f-search" className="cd-label">Search</label>
            <input
              id="cd-f-search"
              type="text"
              className="cd-input"
              placeholder="CusDec number or company"
              value={search}
              onChange={(e) => onChange('search', e.target.value)}
            />
          </div>

          {/* Block 2 — Customs office */}
          <div className="cd-filter-block cd-filter-block--office">
            <label htmlFor="cd-f-office" className="cd-label">Customs office</label>
            <select
              id="cd-f-office"
              className="cd-select"
              value={office}
              onChange={(e) => onChange('office', e.target.value)}
            >
              <option value="">All offices</option>
              {offices.map((o) => <option key={o} value={o}>{o}</option>)}
            </select>
          </div>

          {/* Block 3 — Date range */}
          <div className="cd-filter-block cd-filter-block--dates">
            <span className="cd-label" aria-hidden="true">{rangeLabel} range</span>
            <div className="cd-date-inputs">
              <input
                id="cd-f-from"
                type="date"
                className="cd-input cd-input--date"
                value={fromDate}
                aria-label={`${rangeLabel} from`}
                onChange={(e) => onChange('fromDate', e.target.value)}
              />
              <span className="cd-date-sep" aria-hidden="true">–</span>
              <input
                id="cd-f-to"
                type="date"
                className="cd-input cd-input--date"
                value={toDate}
                aria-label={`${rangeLabel} to`}
                onChange={(e) => onChange('toDate', e.target.value)}
              />
            </div>
          </div>

          {/* More filters — not a block */}
          <button
            type="button"
            className={`cd-mf-btn${advancedCount > 0 ? ' cd-mf-btn--active' : ''}`}
            aria-expanded={panelOpen}
            aria-controls="cd-mf-panel"
            onClick={() => setPanelOpen((o) => !o)}
          >
            <SlidersHorizontal size={14} aria-hidden="true" />
            More filters
            {advancedCount > 0 && (
              <span className="cd-mf-badge" aria-label={`${advancedCount} active`}>{advancedCount}</span>
            )}
            <ChevronDown
              size={12}
              aria-hidden="true"
              className={`cd-mf-chevron${panelOpen ? ' cd-mf-chevron--open' : ''}`}
            />
          </button>

          {/* Clear — reserved slot, no reflow */}
          <button
            type="button"
            className="cd-clear-btn"
            style={{ visibility: hasAny ? 'visible' : 'hidden' }}
            aria-hidden={!hasAny}
            tabIndex={hasAny ? 0 : -1}
            onClick={onClear}
          >
            <X size={13} aria-hidden="true" />
            Clear all
          </button>
        </div>
      )}

      {/* ── Mobile row: search + Filters button + Clear ───────────────────── */}
      {isMobile && (
        <div className="cd-filterbar-row cd-filterbar-row--mobile">
          <div className="cd-filter-field cd-filter-field--search-full">
            <label htmlFor="cd-fm-search" className="cd-label">Search</label>
            <input
              id="cd-fm-search"
              type="text"
              className="cd-input"
              placeholder="Search CusDec number or company"
              value={search}
              onChange={(e) => onChange('search', e.target.value)}
            />
          </div>

          <button
            type="button"
            className={`cd-mf-btn${mobileFilterCount > 0 ? ' cd-mf-btn--active' : ''}`}
            aria-expanded={panelOpen}
            aria-controls="cd-mf-panel"
            onClick={() => setPanelOpen((o) => !o)}
          >
            <SlidersHorizontal size={14} aria-hidden="true" />
            Filters
            {mobileFilterCount > 0 && (
              <span className="cd-mf-badge" aria-label={`${mobileFilterCount} active`}>{mobileFilterCount}</span>
            )}
          </button>

          <button
            type="button"
            className="cd-clear-btn"
            style={{ visibility: hasAny ? 'visible' : 'hidden' }}
            aria-hidden={!hasAny}
            tabIndex={hasAny ? 0 : -1}
            onClick={onClear}
          >
            <X size={13} aria-hidden="true" />
            Clear
          </button>
        </div>
      )}

      {/* ── Single panel (content differs by breakpoint) ──────────────────── */}
      <AnimatePresence>
        {panelOpen && (
          <motion.div
            id="cd-mf-panel"
            className="cd-mf-panel"
            role="group"
            aria-label={isMobile ? 'Filters' : 'More filters'}
            {...panelMotion}
          >
            <div className="cd-mf-panel__grid">
              {/* Mobile-only: office + date range */}
              {isMobile && (
                <>
                  <div className="cd-filter-field">
                    <label htmlFor="cd-fp-office" className="cd-label">Customs office</label>
                    <select
                      id="cd-fp-office"
                      className="cd-select"
                      value={office}
                      onChange={(e) => onChange('office', e.target.value)}
                    >
                      <option value="">All offices</option>
                      {offices.map((o) => <option key={o} value={o}>{o}</option>)}
                    </select>
                  </div>

                  <div className="cd-date-group">
                    <span className="cd-label" aria-hidden="true">{rangeLabel} range</span>
                    <div className="cd-date-inputs">
                      <input
                        id="cd-fp-from"
                        type="date"
                        className="cd-input cd-input--date"
                        value={fromDate}
                        aria-label={`${rangeLabel} from`}
                        onChange={(e) => onChange('fromDate', e.target.value)}
                      />
                      <span className="cd-date-sep" aria-hidden="true">–</span>
                      <input
                        id="cd-fp-to"
                        type="date"
                        className="cd-input cd-input--date"
                        value={toDate}
                        aria-label={`${rangeLabel} to`}
                        onChange={(e) => onChange('toDate', e.target.value)}
                      />
                    </div>
                  </div>
                </>
              )}

              {/* Both: CusDec number + counterparty TIN */}
              <div className="cd-filter-field">
                <label htmlFor={isMobile ? 'cd-fp-cdnum' : 'cd-f-cdnum'} className="cd-label">CusDec number</label>
                <input
                  id={isMobile ? 'cd-fp-cdnum' : 'cd-f-cdnum'}
                  type="text"
                  className="cd-input"
                  placeholder="Exact or partial match"
                  value={cusDecNumber}
                  onChange={(e) => onChange('cusDecNumber', e.target.value)}
                />
                <span className="cd-field-hint" aria-hidden="true" />
              </div>

              <div className="cd-filter-field">
                <label htmlFor={isMobile ? 'cd-fp-tin' : 'cd-f-tin'} className="cd-label">{tinLabel}</label>
                <input
                  id={isMobile ? 'cd-fp-tin' : 'cd-f-tin'}
                  type="text"
                  className="cd-input"
                  placeholder="Starts with…"
                  value={counterpartyTin}
                  maxLength={14}
                  onChange={(e) => onChange('counterpartyTin', e.target.value)}
                  aria-describedby={isMobile ? 'cd-fp-tin-hint' : 'cd-tin-hint'}
                />
                <span id={isMobile ? 'cd-fp-tin-hint' : 'cd-tin-hint'} className="cd-field-hint">
                  9-digit TIN or 14-character extended TIN
                </span>
              </div>

              {/* Mobile-only: close button */}
              {isMobile && (
                <div className="cd-mf-panel__close-row">
                  <button
                    type="button"
                    className="cd-mf-close-btn"
                    onClick={() => setPanelOpen(false)}
                  >
                    Close filters
                  </button>
                </div>
              )}
            </div>
          </motion.div>
        )}
      </AnimatePresence>

      {/* ── Active filter chips ───────────────────────────────────────────── */}
      <AnimatePresence>
        {chips.length > 0 && (
          <motion.div
            className="cd-chips"
            aria-label="Active filters"
            {...chipsMotion}
          >
            {chips.map((chip) => (
              <button
                key={chip.key}
                type="button"
                className="cd-chip"
                onClick={chip.clear}
                aria-label={`Remove filter: ${chip.label}`}
              >
                {chip.label} <X size={11} aria-hidden="true" />
              </button>
            ))}
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
