import { useNavigate } from 'react-router-dom';
import { Shield, Info } from 'lucide-react';
import { Pill } from '../../components/ui';
import { formatDate, daysUntil } from './formatHelpers';

function TypePill({ type }) {
  if (type === 'Declarant')
    return (
      <span className="cd-type-pill cd-type-pill--declarant">
        <Shield size={11} aria-hidden="true" /> Declarant
      </span>
    );
  return (
    <span className="cd-type-pill cd-type-pill--consignee">
      <Shield size={11} aria-hidden="true" /> Consignee
    </span>
  );
}

export default function SubscriptionStrip({ strip, subscriptions, portal }) {
  const navigate = useNavigate();
  const days = daysUntil(strip.expiryDate);
  const workspaceBase = `/${portal}/cusdec`;

  function handleSwitch(e) {
    const id = e.target.value;
    if (id && id !== String(strip.memberSubscriptionId)) {
      navigate(`${workspaceBase}/${id}`);
    }
  }

  const hasMultiple = subscriptions && subscriptions.length > 1;

  return (
    <div className="cd-strip">
      <div className="cd-strip__left">
        <TypePill type={strip.subscriptionType} />
        <span className="cd-strip__company">{strip.companyName}</span>

        <span className="cd-strip__sep" aria-hidden="true">|</span>
        <span className="cd-strip__meta">TIN {strip.companyTin}</span>

        <span className="cd-strip__sep" aria-hidden="true">|</span>
        <span className="cd-strip__meta">
          <span className="cd-strip__label">Plan</span>
          {strip.planName}
        </span>

        <span className="cd-strip__sep" aria-hidden="true">|</span>
        <span className="cd-strip__meta">
          Expires {formatDate(strip.expiryDate)}
        </span>
        {days !== null && days <= 90 && (
          <Pill tone={days <= 30 ? 'amber' : 'green'} className="cd-strip__expiry-pill">
            {days}d
          </Pill>
        )}

        {strip.containersBlocked && (
          <>
            <span
              className="cd-strip__ack-pill"
              title="This plan is acknowledgement only. Container details are not available."
              aria-describedby="cd-ack-pill-desc"
            >
              <Info size={11} aria-hidden="true" />
              Acknowledgement only
            </span>
            <span id="cd-ack-pill-desc" className="cd-visually-hidden">
              This plan is acknowledgement only. Container details are not available.
            </span>
          </>
        )}
      </div>

      {hasMultiple && (
        <div className="cd-strip__switcher">
          <label htmlFor="cd-sub-switch" className="cd-strip__switch-label">
            Subscription
          </label>
          <select
            id="cd-sub-switch"
            className="cd-select cd-select--sm"
            value={String(strip.memberSubscriptionId)}
            onChange={handleSwitch}
            aria-label="Switch subscription"
          >
            {subscriptions.map((s) => (
              <option key={s.memberSubscriptionId} value={String(s.memberSubscriptionId)}>
                {s.subscriptionType} · {s.companyName} · {s.planName}
              </option>
            ))}
          </select>
        </div>
      )}
    </div>
  );
}
