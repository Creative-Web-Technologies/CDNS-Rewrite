import { Check, Ban, Package, FileText, Route, Loader2 } from 'lucide-react';
import { Pill, Button } from '../../components/ui';
import { formatDate, formatDatetime } from './formatHelpers';

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
  const { tab, state, lastUpdate, completedAt, completedByName, disclaimedDate, disclaimedByName } = item;

  if (tab === 'tracking' && state === 'new') {
    return <span className="cd-row__context">Awaiting your decision</span>;
  }
  if (tab === 'tracking' && state === 'inprogress') {
    const text = lastUpdate ? `Last update ${formatDatetime(lastUpdate)}` : '—';
    return <span className="cd-row__context cd-row__context--nowrap" title={text}>{text}</span>;
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
  onReopen,
  onOpenModal,
}) {
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
        <div className="cd-row__meta-row">
          <span className="cd-row__regdate">Registered {formatDate(item.cusDecDate)}</span>
          {item.office && <span className="cd-row__office-chip">{item.office}</span>}
        </div>
      </div>

      {/* Counterparty */}
      <div className="cd-row__party" aria-label={`${cpLabel}: ${cpName}`}>
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
          onAcknowledge={onAcknowledge}
          onDisclaimRequest={onDisclaimRequest}
          onReopen={onReopen}
          onOpenModal={onOpenModal}
        />
      </div>
    </article>
  );
}

function RowActions({
  item, isBusy, anyBusy, containersBlocked,
  onAcknowledge, onDisclaimRequest, onReopen, onOpenModal,
}) {
  const { tab, state, cusDecId } = item;

  if (tab === 'tracking' && state === 'new') {
    return (
      <div className="cd-row__action-group">
        <Button
          size="sm"
          variant="secondary"
          icon={FileText}
          disabled={anyBusy}
          onClick={() => onOpenModal(cusDecId, 'details')}
        >
          Details
        </Button>
        <Button
          size="sm"
          icon={isBusy ? undefined : Check}
          disabled={anyBusy}
          onClick={() => onAcknowledge(cusDecId)}
          aria-busy={isBusy}
        >
          {isBusy
            ? <><Loader2 size={14} className="cd-spin" aria-hidden="true" /> Acknowledging…</>
            : 'Acknowledge'}
        </Button>
        <Button
          size="sm"
          variant="secondary"
          className="cd-btn-disclaim"
          icon={Ban}
          disabled={anyBusy}
          onClick={() => onDisclaimRequest(item)}
        >
          Disclaim
        </Button>
      </div>
    );
  }

  if (tab === 'tracking' && state === 'inprogress') {
    return (
      <div className="cd-row__action-group">
        {!containersBlocked && (
          <Button
            size="sm"
            variant="secondary"
            icon={Package}
            disabled={anyBusy}
            onClick={() => onOpenModal(cusDecId, 'containers')}
          >
            Containers
          </Button>
        )}
        <Button
          size="sm"
          variant="secondary"
          icon={FileText}
          disabled={anyBusy}
          onClick={() => onOpenModal(cusDecId, 'details')}
        >
          Details
        </Button>
        <Button
          size="sm"
          variant="secondary"
          icon={Route}
          disabled={anyBusy}
          onClick={() => onOpenModal(cusDecId, 'tracking')}
        >
          Tracking
        </Button>
      </div>
    );
  }

  if (tab === 'completed') {
    return (
      <div className="cd-row__action-group">
        <Button
          size="sm"
          variant="secondary"
          icon={FileText}
          disabled={anyBusy}
          onClick={() => onOpenModal(cusDecId, 'details')}
        >
          Details
        </Button>
        <Button
          size="sm"
          variant="secondary"
          icon={Route}
          disabled={anyBusy}
          onClick={() => onOpenModal(cusDecId, 'tracking')}
        >
          Tracking
        </Button>
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
      </div>
    );
  }

  if (tab === 'disclaimed') {
    return (
      <div className="cd-row__action-group">
        <Button
          size="sm"
          variant="secondary"
          icon={FileText}
          disabled={anyBusy}
          onClick={() => onOpenModal(cusDecId, 'details')}
        >
          Details
        </Button>
      </div>
    );
  }

  return null;
}
