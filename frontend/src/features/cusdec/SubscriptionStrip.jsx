import { useNavigate } from 'react-router-dom';
import { Pill } from '../../components/ui';
import { formatDate, daysUntil } from './formatHelpers';

function stripAckSuffix(name) {
  return name?.replace(/\s*\(Acknowledgement Only\)\s*/i, '').trim() || name || '';
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

        {/* Line 1: company name */}
        <div className="cd-strip__row1">
          <span className="cd-strip__company">{strip.companyName}</span>
        </div>

        {/* Line 2: TIN · Plan · Expires */}
        <div className="cd-strip__row2">
          <div className="cd-strip__meta-item">
            <span className="cd-strip__meta-label">TIN</span>
            <span className="cd-strip__meta-value">{strip.companyTin}</span>
          </div>
          <div className="cd-strip__meta-sep" aria-hidden="true" />
          <div className="cd-strip__meta-item">
            <span className="cd-strip__meta-label">Plan</span>
            <span className="cd-strip__meta-value">{strip.planName}</span>
          </div>
          <div className="cd-strip__meta-sep" aria-hidden="true" />
          <div className="cd-strip__meta-item">
            <span className="cd-strip__meta-label">Expires</span>
            <span className="cd-strip__meta-value cd-strip__meta--expiry">
              {formatDate(strip.expiryDate)}
              {days !== null && days <= 90 && (
                <Pill
                  tone={days <= 30 ? 'amber' : 'green'}
                  className="cd-strip__expiry-pill"
                >
                  {days}d
                </Pill>
              )}
            </span>
          </div>
        </div>

      </div>

      {hasMultiple && (
        <div className="cd-strip__switcher">
          <label htmlFor="cd-sub-switch" className="cd-strip__switch-label">
            Subscription
          </label>
          <select
            id="cd-sub-switch"
            className="cd-select cd-strip__select"
            value={String(strip.memberSubscriptionId)}
            onChange={handleSwitch}
            aria-label="Switch subscription"
            title={`${strip.subscriptionType} · ${strip.companyName} · ${strip.planName}`}
          >
            {subscriptions.map((s) => {
              const sIsAck = s.planName?.toLowerCase().includes('acknowledgement only');
              const cleanPlan = sIsAck ? stripAckSuffix(s.planName) : s.planName;
              const label = `${s.subscriptionType} · ${s.companyName} · ${cleanPlan}${sIsAck ? ' (Ack only)' : ''}`;
              return (
                <option key={s.memberSubscriptionId} value={String(s.memberSubscriptionId)}>
                  {label}
                </option>
              );
            })}
          </select>
        </div>
      )}
    </div>
  );
}
