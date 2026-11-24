import { forwardRef, useRef } from 'react';
import { Check } from 'lucide-react';
import { Button } from '../../../components/ui';
import { formatAmount, formatDuration } from './planUtils';
import './plan-card.css';

function toDays(plan) {
  // Lifetime sorts after every finite plan.
  if (plan.durationType === 'Lifetime') return Infinity;
  if (plan.durationType === 'Years') return plan.noOfActiveDuration * 365;
  if (plan.durationType === 'Months') return plan.noOfActiveDuration * 30;
  return plan.noOfActiveDuration;
}

/**
 * variant="radio"  — entire card is clickable; pass selected/onSelect.
 *                    Uses role="radio"; arrow-key nav handled by onKeyDown.
 * variant="button" — footer "Select" button; pass onSelect.
 * compact          — compact single-row layout for narrow column (radio only).
 */
export const PlanCard = forwardRef(function PlanCard(
  { plan, selected, onSelect, variant = 'button', compact = false, tabIndex, onKeyDown },
  ref,
) {
  const isFree = plan.amount === 0;
  const isRadio = variant === 'radio';

  const sharedProps = {
    ref,
    role: isRadio ? 'radio' : undefined,
    'aria-checked': isRadio ? !!selected : undefined,
    tabIndex: isRadio ? (tabIndex ?? 0) : undefined,
    onClick: isRadio ? () => onSelect(plan) : undefined,
    onKeyDown: isRadio
      ? (e) => {
          if (e.key === ' ' || e.key === 'Enter') { e.preventDefault(); onSelect(plan); }
          onKeyDown?.(e);
        }
      : undefined,
  };

  if (compact) {
    return (
      <div
        {...sharedProps}
        className={`pc-card pc-card--compact${isRadio ? ' pc-card--radio' : ''}${selected ? ' is-selected' : ''}`}
      >
        <div className="pc-compact-row">
          <div className="pc-compact-info">
            <span className="pc-compact-name">{plan.subscriptionName}</span>
            <span className="pc-compact-meta">
              {formatDuration(plan.noOfActiveDuration, plan.durationType)}
              {' · '}
              {plan.actionType === 'AcknowledgementOnly' ? 'Only For Acknowledgement' : 'General'}
            </span>
          </div>
          <div className="pc-compact-right">
            <span className={`pc-compact-amount${isFree ? ' is-free' : ''}`}>
              {formatAmount(plan.amount)}
            </span>
            {isRadio && selected && <Check size={14} className="pc-check" aria-hidden="true" />}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div
      {...sharedProps}
      className={`pc-card${isRadio ? ' pc-card--radio' : ''}${selected ? ' is-selected' : ''}`}
    >
      <div className="pc-header">
        <p className="pc-name">{plan.subscriptionName}</p>
        {isRadio && selected && <Check size={18} className="pc-check" aria-hidden="true" />}
      </div>
      <div className="pc-meta">
        <span>Active {formatDuration(plan.noOfActiveDuration, plan.durationType)}</span>
        <span>{plan.subscriptionType}</span>
      </div>
      {plan.description && <p className="pc-desc">{plan.description}</p>}
      <div className={`pc-amount${isFree ? ' is-free' : ''}`}>
        {formatAmount(plan.amount)}
      </div>
      <div className="pc-footer">
        <span className="pc-applicable">
          {plan.actionType === 'AcknowledgementOnly' ? 'Only For Acknowledgement' : 'General'}
        </span>
        {!isRadio && <Button onClick={() => onSelect(plan)}>Select</Button>}
      </div>
    </div>
  );
});

export function PlanRadioGroup({ plans, selectedPlan, onSelect, compact = false }) {
  const cardRefs = useRef({});
  const selectedId = selectedPlan?.subscriptionId;
  const sortedPlans = [...plans].sort((a, b) => toDays(a) - toDays(b));

  function handleArrow(idx, e) {
    if (
      e.key !== 'ArrowDown' && e.key !== 'ArrowRight' &&
      e.key !== 'ArrowUp'  && e.key !== 'ArrowLeft'
    ) return;
    e.preventDefault();
    const next =
      e.key === 'ArrowDown' || e.key === 'ArrowRight'
        ? (idx + 1) % sortedPlans.length
        : (idx - 1 + sortedPlans.length) % sortedPlans.length;
    onSelect(sortedPlans[next]);
    cardRefs.current[sortedPlans[next].subscriptionId]?.focus();
  }

  return (
    <div className={`pc-grid${compact ? ' pc-grid--compact' : ''}`} role="radiogroup">
      {sortedPlans.map((plan, idx) => (
        <PlanCard
          key={plan.subscriptionId}
          ref={(el) => { cardRefs.current[plan.subscriptionId] = el; }}
          plan={plan}
          selected={plan.subscriptionId === selectedId}
          onSelect={onSelect}
          variant="radio"
          compact={compact}
          tabIndex={
            plan.subscriptionId === selectedId ? 0
              : selectedId == null && idx === 0 ? 0
              : -1
          }
          onKeyDown={(e) => handleArrow(idx, e)}
        />
      ))}
    </div>
  );
}
