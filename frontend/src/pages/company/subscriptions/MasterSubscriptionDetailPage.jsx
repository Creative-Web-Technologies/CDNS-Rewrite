import { useState, useEffect } from 'react';
import { useNavigate, Navigate } from 'react-router-dom';
import { ArrowLeft, AlertCircle } from 'lucide-react';
import { Button, Card, PageTransition, Pill } from '../../../components/ui';
import { apiGet, apiPost } from '../../../lib/api';
import { useNumericIdParam } from '../../../lib/useNumericIdParam';
import './master-subscription-detail.css';

function formatDate(iso) {
  if (!iso) return '—';
  return new Date(iso).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
}

function formatDateTime(iso) {
  if (!iso) return '—';
  return new Date(iso).toLocaleString('en-GB', {
    day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit',
  });
}

function statusTone(status) {
  if (status === 'Active') return 'green';
  if (status === 'Deactivated') return 'red';
  if (status === 'Expired') return 'grey';
  if (status === 'Suspended') return 'amber';
  return 'grey';
}

function DetailRow({ label, value }) {
  return (
    <div className="msd-row">
      <span className="msd-label">{label}</span>
      <span className="msd-value">{value ?? '—'}</span>
    </div>
  );
}

export default function MasterSubscriptionDetailPage() {
  const id = useNumericIdParam();
  const navigate = useNavigate();
  const [sub, setSub] = useState(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(null);

  // Deactivate
  const [deactivateOpen, setDeactivateOpen] = useState(false);
  const [comment, setComment] = useState('');
  const [deactivating, setDeactivating] = useState(false);
  const [deactivateError, setDeactivateError] = useState(null);
  const [deactivateConflict, setDeactivateConflict] = useState(false);

  useEffect(() => {
    if (id === null) return;
    let alive = true;
    setLoading(true);
    apiGet(`/api/master/subscriptions/${id}`)
      .then((data) => { if (alive) { setSub(data); setLoading(false); } })
      .catch((err) => { if (alive) { setLoadError(err.status === 404 ? 'not_found' : 'error'); setLoading(false); } });
    return () => { alive = false; };
  }, [id]);

  async function handleDeactivate() {
    if (!comment.trim()) return;
    setDeactivating(true);
    setDeactivateError(null);
    setDeactivateConflict(false);
    try {
      await apiPost(`/api/master/subscriptions/${id}/deactivate`, { comment: comment.trim() });
      setSub((s) => ({ ...s, status: 'Deactivated', isActive: false }));
      setDeactivateOpen(false);
      setComment('');
    } catch (err) {
      if (err.status === 409) {
        setDeactivateConflict(true);
        setDeactivateError('This subscription is not currently active and cannot be deactivated.');
      } else if (err.status === 404) {
        setDeactivateError('Subscription not found.');
      } else {
        setDeactivateError('Something went wrong. Please try again.');
      }
    } finally {
      setDeactivating(false);
    }
  }

  if (id === null) return <Navigate to="/company/subscriptions" replace />;

  if (loading) {
    return (
      <PageTransition>
        <div className="msd-loading" aria-busy="true">Loading…</div>
      </PageTransition>
    );
  }

  if (loadError === 'not_found' || (!loading && !sub)) {
    return (
      <PageTransition>
        <div className="msd-stack">
          <button type="button" className="msd-back" onClick={() => navigate('/company/subscriptions')}>
            <ArrowLeft size={16} aria-hidden="true" /> Back to subscriptions
          </button>
          <Card>
            <div className="msd-not-found">
              <p>Subscription not found or you do not have permission to view it.</p>
              <Button variant="secondary" onClick={() => navigate('/company/subscriptions')}>Back to subscriptions</Button>
            </div>
          </Card>
        </div>
      </PageTransition>
    );
  }

  const isActive = sub?.isActive;

  return (
    <PageTransition>
      <div className="msd-stack">
        <div className="msd-top">
          <button type="button" className="msd-back" onClick={() => navigate('/company/subscriptions')}>
            <ArrowLeft size={16} aria-hidden="true" /> Back to subscriptions
          </button>
        </div>

        <div className="msd-header">
          <div>
            <h1 className="msd-title">
              Subscription {sub.subscriptionNumber ?? `#${sub.memberSubscriptionId}`}
            </h1>
            <p className="msd-subtitle">Member subscription details and history.</p>
          </div>
          <Pill tone={statusTone(sub.status)}>{sub.status}</Pill>
        </div>

        <div className="msd-layout">
          <div>
            <Card title="Subscription details">
              <div className="msd-details">
                <DetailRow label="Subscription no." value={sub.subscriptionNumber} />
                <DetailRow label="Plan" value={sub.subscriptionName} />
                <DetailRow label="Amount" value={sub.subscriptionAmount != null ? `${sub.subscriptionAmount}` : '—'} />
                <DetailRow label="Member" value={sub.memberName} />
                <DetailRow label="Member email" value={sub.memberEmail} />
                <DetailRow label="Company" value={sub.companyName} />
                <DetailRow label="TIN" value={sub.companyTin} />
                <DetailRow label="Subscription type" value={sub.subscriptionType} />
                <DetailRow label="Start date" value={formatDate(sub.startDate)} />
                <DetailRow label="Expiry date" value={formatDate(sub.expiryDate)} />
                {sub.requestNumber && <DetailRow label="Request no." value={sub.requestNumber} />}
              </div>
            </Card>

            {sub.statusHistory && sub.statusHistory.length > 0 && (
              <Card title="Status history" style={{ marginTop: 20 }}>
                <div className="msd-history">
                  {sub.statusHistory.map((h) => (
                    <div key={h.id} className="msd-history-row">
                      <span className="msd-history-status">{h.status}</span>
                      <span className="msd-history-comment">{h.comment ?? '—'}</span>
                      <span className="msd-history-date">{formatDateTime(h.createdDateTime)}</span>
                    </div>
                  ))}
                </div>
              </Card>
            )}
          </div>

          {isActive && (
            <aside>
              <Card title="Deactivate">
                <p className="msd-deactivate-info">
                  Deactivating this subscription will prevent the member from accessing CDNS data.
                  A comment is required.
                </p>

                {deactivateConflict && (
                  <div className="msd-conflict-msg" role="alert">
                    {deactivateError}
                  </div>
                )}

                {!deactivateOpen ? (
                  <Button
                    variant="secondary"
                    style={{ borderColor: 'var(--pill-red-fg)', color: 'var(--pill-red-fg)' }}
                    onClick={() => setDeactivateOpen(true)}
                  >
                    Deactivate…
                  </Button>
                ) : (
                  <div className="msd-confirm">
                    <p className="msd-confirm-text">
                      Are you sure you want to deactivate this subscription?
                    </p>
                    <div className="msd-field">
                      <label htmlFor="msd-comment" className="msd-field-label">
                        Reason (required)
                      </label>
                      <textarea
                        id="msd-comment"
                        className="msd-textarea"
                        rows={3}
                        placeholder="State the reason for deactivation…"
                        value={comment}
                        onChange={(e) => setComment(e.target.value)}
                      />
                    </div>
                    {deactivateError && !deactivateConflict && (
                      <div className="msd-error" role="alert">
                        <AlertCircle size={14} aria-hidden="true" /> {deactivateError}
                      </div>
                    )}
                    <div className="msd-confirm-actions">
                      <Button
                        variant="danger"
                        disabled={!comment.trim() || deactivating}
                        onClick={handleDeactivate}
                      >
                        {deactivating ? 'Deactivating…' : 'Yes, deactivate'}
                      </Button>
                      <Button variant="secondary" onClick={() => { setDeactivateOpen(false); setDeactivateError(null); }}>
                        Cancel
                      </Button>
                    </div>
                  </div>
                )}
              </Card>
            </aside>
          )}
        </div>
      </div>
    </PageTransition>
  );
}
