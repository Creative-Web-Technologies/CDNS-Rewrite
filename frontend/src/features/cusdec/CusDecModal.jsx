import { useEffect, useState } from 'react';
import { Loader2 } from 'lucide-react';
import { motion, useReducedMotion } from 'motion/react';
import Modal from '../../components/ui/Modal';
import { Button, Pill } from '../../components/ui';
import { formatDate, formatDatetime } from './formatHelpers';
import { cusDecFeatures, COMPLETE_REQUIRES_EXIT } from './features';

const MODAL_TABS = [
  { key: 'details',    label: 'Details' },
  { key: 'tracking',   label: 'Tracking' },
  { key: 'containers', label: 'Containers' },
];

function counterpartyFor(item, subscriptionType) {
  if (!item) return { label: 'Counterparty', name: '—' };
  if (subscriptionType === 'Declarant') return { label: 'Consignee', name: item.consigneeName || '—' };
  return { label: 'Declarant', name: item.declarantName || '—' };
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
  containersBlocked,
  busyId,
  onComplete,
  onClose,
}) {
  // Keep the last item while the exit animation plays so title/content stay populated.
  const [lastItem, setLastItem] = useState(null);
  useEffect(() => { if (item) setLastItem(item); }, [item]);
  const displayed = item ?? lastItem;

  const [activeTab, setActiveTab] = useState(initialTab ?? 'details');
  useEffect(() => { if (item) setActiveTab(initialTab ?? 'details'); }, [item, initialTab]);

  const shouldReduce = useReducedMotion();

  const isBusy = busyId === displayed?.cusDecId;
  const { label: cpLabel, name: cpName } = counterpartyFor(displayed, subscriptionType);

  const visibleTabs = MODAL_TABS.filter(
    (t) => !(t.key === 'containers' && containersBlocked),
  );

  const showCompleteBtn =
    displayed?.tab === 'tracking' &&
    displayed?.state === 'inprogress' &&
    activeTab === 'tracking' &&
    (!COMPLETE_REQUIRES_EXIT || displayed?.isExited);

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

  const footer = showCompleteBtn ? (
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
  ) : undefined;

  return (
    <Modal
      open={!!item}
      onClose={onClose}
      title={displayed?.cusDecNumber ?? '—'}
      subtitle={displayed ? `${cpLabel}: ${cpName}${displayed.office ? ` · ${displayed.office}` : ''}` : undefined}
      size="lg"
      tabs={tabBar}
      footer={footer}
    >
      {displayed && (
        <div
          role="tabpanel"
          id={`cdm-panel-${activeTab}`}
          aria-labelledby={`cdm-tab-${activeTab}`}
        >
          {activeTab === 'details' && <DetailsTab />}
          {activeTab === 'tracking' && <TrackingTab item={displayed} />}
          {activeTab === 'containers' && <ContainersTab />}
        </div>
      )}
    </Modal>
  );
}

function DetailsTab() {
  if (!cusDecFeatures.detail) {
    return (
      <div className="cdm-empty-state">
        <p className="cdm-empty-state__text">This view is not connected yet.</p>
        {/* TODO: fetch detail endpoint when cusDecFeatures.detail is true */}
      </div>
    );
  }
  return (
    <div>
      {/* TODO: fetch and render detail data from API when cusDecFeatures.detail is true */}
    </div>
  );
}

function TrackingTab({ item }) {
  const { tab, state, lastUpdate, office, claimedDate, claimedByName } = item;

  return (
    <div className="cdm-tracking">
      <div className="cdm-tracking__summary">
        <div className="cdm-tracking__row">
          <span className="cdm-tracking__label">Status</span>
          <div className="cdm-tracking__pills">
            <StatusPill item={item} />
            {item.isExited && tab === 'tracking' && state === 'inprogress' && (
              <Pill tone="green">Exited</Pill>
            )}
          </div>
        </div>
        {lastUpdate && (
          <div className="cdm-tracking__row">
            <span className="cdm-tracking__label">Last update</span>
            <span className="cdm-tracking__value">{formatDatetime(lastUpdate)}</span>
          </div>
        )}
        {office && (
          <div className="cdm-tracking__row">
            <span className="cdm-tracking__label">Customs office</span>
            <span className="cdm-tracking__value">{office}</span>
          </div>
        )}
        {claimedDate && (
          <div className="cdm-tracking__row">
            <span className="cdm-tracking__label">Acknowledged</span>
            <span className="cdm-tracking__value">
              {formatDate(claimedDate)}{claimedByName ? ` by ${claimedByName}` : ''}
            </span>
          </div>
        )}
      </div>
      {!cusDecFeatures.status && (
        <p className="cdm-tracking__note">
          Full status timeline will appear here once tracking is connected.
          {/* TODO: fetch and render full timeline when cusDecFeatures.status is true */}
        </p>
      )}
    </div>
  );
}

function ContainersTab() {
  if (!cusDecFeatures.containers) {
    return (
      <div className="cdm-empty-state">
        <p className="cdm-empty-state__text">This view is not connected yet.</p>
        {/* TODO: fetch containers endpoint when cusDecFeatures.containers is true */}
      </div>
    );
  }
  return (
    <div>
      {/* TODO: fetch and render containers data from API when cusDecFeatures.containers is true */}
    </div>
  );
}
