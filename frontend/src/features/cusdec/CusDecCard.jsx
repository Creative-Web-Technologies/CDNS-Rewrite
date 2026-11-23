import { useState } from 'react';
import { MoreVertical, Loader2 } from 'lucide-react';
import { Pill, Button } from '../../components/ui';
import { formatDate, formatDatetime } from './formatHelpers';
import { cusDecFeatures } from './features';

function StatusPill({ item }) {
  const { tab, state, currentStatus } = item;
  if (tab === 'disclaimed') return <Pill tone="red">Disclaimed</Pill>;
  if (tab === 'completed') {
    const tone = currentStatus?.toLowerCase().includes('cancel') ? 'grey' : 'green';
    return <Pill tone={tone}>{currentStatus || 'Completed'}</Pill>;
  }
  if (state === 'new') return <Pill tone="amber">New</Pill>;
  return <Pill tone="blue">{currentStatus || 'In progress'}</Pill>;
}

function ContextLine({ item }) {
  const { tab, state, claimedDate, claimedByName, lastUpdate, completedAt, completedByName, disclaimedDate, disclaimedByName } = item;

  if (tab === 'tracking' && state === 'new') {
    return <span className="cd-row__context">Awaiting your decision</span>;
  }
  if (tab === 'tracking' && state === 'inprogress') {
    const ackPart = claimedDate
      ? `Acknowledged ${formatDate(claimedDate)}${claimedByName ? ` by ${claimedByName}` : ''}`
      : '';
    const updatePart = lastUpdate ? `Last update ${formatDatetime(lastUpdate)}` : '';
    const text = [ackPart, updatePart].filter(Boolean).join(' · ');
    return <span className="cd-row__context">{text || '—'}</span>;
  }
  if (tab === 'completed') {
    const datePart = completedAt ? formatDate(completedAt) : '—';
    const byPart = completedByName ? ` by ${completedByName}` : '';
    return <span className="cd-row__context">Completed {datePart}{byPart}</span>;
  }
  if (tab === 'disclaimed') {
    const datePart = disclaimedDate ? formatDate(disclaimedDate) : '—';
    const byPart = disclaimedByName ? ` by ${disclaimedByName}` : '';
    return <span className="cd-row__context">Disclaimed {datePart}{byPart}</span>;
  }
  return null;
}

function counterpartyFor(item, subscriptionType) {
  if (subscriptionType === 'Declarant') {
    return { label: 'Consignee', name: item.consigneeName || '—' };
  }
  return { label: 'Declarant', name: item.declarantName || '—' };
}

function getRowState(item) {
  if (item.tab === 'disclaimed') return 'disclaimed';
  if (item.tab === 'completed') return 'completed';
  if (item.state === 'new') return 'new';
  return 'inprogress';
}

export default function CusDecCard({
  item,
  subscriptionType,
  containersBlocked,
  busyCardId,
  highlighted,
  onAcknowledge,
  onDisclaimRequest,
  onComplete,
  onReopen,
}) {
  const [overflowOpen, setOverflowOpen] = useState(false);
  const isBusy = busyCardId === item.cusDecId;
  const anyBusy = busyCardId !== null;
  const { label: cpLabel, name: cpName } = counterpartyFor(item, subscriptionType);
  const rowState = getRowState(item);

  const articleClass = [
    'cd-row',
    `cd-row--${rowState}`,
    highlighted ? 'cd-row--highlighted' : '',
    isBusy ? 'cd-row--busy' : '',
  ].filter(Boolean).join(' ');

  return (
    <article className={articleClass} aria-busy={isBusy || undefined}>
      {/* Identity */}
      <div className="cd-row__identity">
        <span className="cd-row__number">{item.cusDecNumber || '—'}</span>
        <span className="cd-row__regdate">Registered {formatDate(item.cusDecDate)}</span>
        {item.office && <span className="cd-row__office-chip">{item.office}</span>}
      </div>

      {/* Counterparty */}
      <div className="cd-row__party">
        <span className="cd-row__party-label">{cpLabel}</span>
        <span className="cd-row__party-name" title={cpName}>{cpName}</span>
      </div>

      {/* Status + context */}
      <div className="cd-row__status">
        <div className="cd-row__status-pills">
          <StatusPill item={item} />
          {item.isExited && item.tab === 'tracking' && item.state === 'inprogress' && (
            <Pill tone="green">Exited</Pill>
          )}
        </div>
        <ContextLine item={item} />
      </div>

      {/* Actions */}
      <div className="cd-row__actions">
        <RowActions
          item={item}
          isBusy={isBusy}
          anyBusy={anyBusy}
          containersBlocked={containersBlocked}
          overflowOpen={overflowOpen}
          setOverflowOpen={setOverflowOpen}
          onAcknowledge={onAcknowledge}
          onDisclaimRequest={onDisclaimRequest}
          onComplete={onComplete}
          onReopen={onReopen}
        />
      </div>
    </article>
  );
}

function RowActions({
  item, isBusy, anyBusy, containersBlocked,
  overflowOpen, setOverflowOpen,
  onAcknowledge, onDisclaimRequest, onComplete, onReopen,
}) {
  const { tab, state, isExited, cusDecId } = item;

  if (tab === 'tracking' && state === 'new') {
    return (
      <div className="cd-row__action-group">
        <Button
          size="sm"
          disabled={anyBusy}
          onClick={() => onAcknowledge(cusDecId)}
          aria-busy={isBusy}
        >
          {isBusy
            ? <><Loader2 size={14} className="cd-spin" aria-hidden="true" /> Acknowledging…</>
            : 'Acknowledge'}
        </Button>
        {cusDecFeatures.detail && (
          <Button variant="secondary" size="sm">View detail</Button>
        )}
        <div className="cd-card__overflow" role="presentation">
          <button
            type="button"
            className="cd-overflow-btn"
            aria-label="More actions"
            aria-expanded={overflowOpen}
            aria-haspopup="menu"
            disabled={anyBusy}
            onClick={() => setOverflowOpen((o) => !o)}
          >
            <MoreVertical size={16} aria-hidden="true" />
          </button>
          {overflowOpen && (
            <OverflowMenu onClose={() => setOverflowOpen(false)}>
              <button
                type="button"
                className="cd-overflow-item cd-overflow-item--danger"
                role="menuitem"
                onClick={() => { setOverflowOpen(false); onDisclaimRequest(item); }}
              >
                Disclaim (not ours)…
              </button>
            </OverflowMenu>
          )}
        </div>
      </div>
    );
  }

  if (tab === 'tracking' && state === 'inprogress') {
    return (
      <div className="cd-row__action-group">
        <Button
          size="sm"
          variant={isExited ? 'primary' : 'secondary'}
          disabled={anyBusy}
          onClick={() => onComplete(cusDecId)}
          aria-busy={isBusy}
        >
          {isBusy
            ? <><Loader2 size={14} className="cd-spin" aria-hidden="true" /> Completing…</>
            : 'Complete'}
        </Button>
        {cusDecFeatures.status && <Button variant="secondary" size="sm">View status</Button>}
        {cusDecFeatures.containers && !containersBlocked && <Button variant="secondary" size="sm">Containers</Button>}
      </div>
    );
  }

  if (tab === 'completed') {
    return (
      <div className="cd-row__action-group">
        <Button
          size="sm"
          variant="secondary"
          disabled={anyBusy}
          onClick={() => onReopen(cusDecId)}
          aria-busy={isBusy}
        >
          {isBusy
            ? <><Loader2 size={14} className="cd-spin" aria-hidden="true" /> Reopening…</>
            : 'Reopen'}
        </Button>
        {cusDecFeatures.status && <Button variant="secondary" size="sm">View status</Button>}
        {cusDecFeatures.containers && !containersBlocked && <Button variant="secondary" size="sm">Containers</Button>}
      </div>
    );
  }

  if (tab === 'disclaimed') {
    return (
      <div className="cd-row__action-group">
        {cusDecFeatures.detail && <Button variant="secondary" size="sm">View detail</Button>}
      </div>
    );
  }

  return null;
}

function OverflowMenu({ children, onClose }) {
  return (
    <div
      className="cd-overflow-menu"
      role="menu"
      onBlur={(e) => { if (!e.currentTarget.contains(e.relatedTarget)) onClose(); }}
    >
      {children}
    </div>
  );
}
