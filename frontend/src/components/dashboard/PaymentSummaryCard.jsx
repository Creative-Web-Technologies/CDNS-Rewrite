import { Landmark, Receipt, ShieldCheck } from 'lucide-react';
import { Card } from '../ui';
import Skeleton from './Skeleton';
import { formatLkr } from './format';

// PaymentSummaryCard (master only) — CIF, tax paid and inspection recoveries
// summed across the master's companies, in LKR.
export default function PaymentSummaryCard({ payment, loading }) {
  const p = payment ?? {};
  const rows = [
    { label: 'Total CIF value', value: p.totalCif, Icon: Landmark },
    { label: 'Total tax paid', value: p.totalTaxPaid, Icon: Receipt },
    { label: 'Inspection recoveries', value: p.inspectionRecoveries, Icon: ShieldCheck },
  ];

  return (
    <Card eyebrow="Payments" title="Payment summary">
      {loading ? (
        <div className="dash-pay">
          {rows.map((r) => <Skeleton key={r.label} h={34} radius={8} />)}
        </div>
      ) : (
        <div className="dash-pay">
          {rows.map(({ label, value, Icon }) => (
            <div className="dash-pay__row" key={label}>
              <span className="dash-pay__label">
                <span className="dash-pay__icon"><Icon size={18} aria-hidden="true" /></span>
                {label}
              </span>
              <span className="dash-pay__value">{formatLkr(value)}</span>
            </div>
          ))}
        </div>
      )}
    </Card>
  );
}
