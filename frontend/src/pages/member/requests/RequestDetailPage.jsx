import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, AlertCircle } from 'lucide-react';
import { Button, Card, PageTransition, Pill } from '../../../components/ui';
import { apiGet, apiPost } from '../../../lib/api';
import './request-detail.css';

function formatDate(iso) {
  if (!iso) return '—';
  return new Date(iso).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
}

function statusTone(status) {
  if (status === 'Pending') return 'amber';
  if (status === 'Approved') return 'green';
  if (status === 'Rejected') return 'red';
  if (status === 'Cancelled') return 'grey';
  return 'grey';
}

function DetailRow({ label, value }) {
  return (
    <div className="rd-row">
      <span className="rd-label">{label}</span>
      <span className="rd-value">{value ?? '—'}</span>
    </div>
  );
}

export default function RequestDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [req, setReq] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [cancelConfirm, setCancelConfirm] = useState(false);
  const [cancelling, setCancelling] = useState(false);
  const [cancelError, setCancelError] = useState(null);

  useEffect(() => {
    let alive = true;
    setLoading(true);
    apiGet(`/api/member/subscription-requests/${id}`)
      .then((data) => { if (alive) { setReq(data); setLoading(false); } })
      .catch((err) => { if (alive) { setError(err.status === 404 ? 'not_found' : 'error'); setLoading(false); } });
    return () => { alive = false; };
  }, [id]);

  async function handleCancel() {
    setCancelling(true);
    setCancelError(null);
    try {
      await apiPost(`/api/member/subscription-requests/${id}/cancel`);
      setReq((r) => ({ ...r, status: 'Cancelled' }));
      setCancelConfirm(false);
    } catch (err) {
      setCancelError(err.code === 'NotCancellable'
        ? 'This request can no longer be cancelled.'
        : 'Something went wrong. Please try again.');
    } finally {
      setCancelling(false);
    }
  }

  if (loading) {
    return (
      <PageTransition>
        <div className="rd-loading" aria-busy="true">Loading…</div>
      </PageTransition>
    );
  }

  if (error === 'not_found' || (!loading && !req)) {
    return (
      <PageTransition>
        <div className="rd-stack">
          <button type="button" className="rd-back" onClick={() => navigate('/member/requests')}>
            <ArrowLeft size={16} aria-hidden="true" /> Back to requests
          </button>
          <Card>
            <div className="rd-not-found">
              <p>Request not found or you don't have permission to view it.</p>
              <Button variant="secondary" onClick={() => navigate('/member/requests')}>Back to requests</Button>
            </div>
          </Card>
        </div>
      </PageTransition>
    );
  }

  const isPending = req?.status === 'Pending';

  return (
    <PageTransition>
      <div className="rd-stack">
        <div className="rd-top">
          <button type="button" className="rd-back" onClick={() => navigate('/member/requests')}>
            <ArrowLeft size={16} aria-hidden="true" /> Back to requests
          </button>
        </div>

        <div className="rd-header">
          <div>
            <h1 className="rd-title">Request {req.requestNumber ?? `#${req.subscriptionRequestId}`}</h1>
            <p className="rd-subtitle">Status and details of one request.</p>
          </div>
          <Pill tone={statusTone(req.status)}>{req.status}</Pill>
        </div>

        <div className="rd-layout">
          <div className="rd-main">
            <Card title="Request details">
              <div className="rd-details">
                <DetailRow label="Request no." value={req.requestNumber} />
                <DetailRow label="Requested on" value={formatDate(req.requestedDateTime)} />
                <DetailRow label="Company" value={req.companyName} />
                <DetailRow label="TIN" value={req.companyTin} />
                <DetailRow label="Subscription type" value={req.subscriptionType} />
                <DetailRow
                  label="Responder"
                  value={req.responderName ?? 'Not yet responded'}
                />
                {req.note && <DetailRow label="Your note" value={req.note} />}
                {req.status === 'Rejected' && req.responderComment && (
                  <DetailRow label="Responder comment" value={req.responderComment} />
                )}
              </div>
            </Card>
          </div>

          {isPending && (
            <aside className="rd-side">
              <Card title="Cancel request">
                <p className="rd-cancel-info">
                  You can cancel while the request is still pending. The company is not notified again.
                </p>

                {!cancelConfirm ? (
                  <Button
                    variant="secondary"
                    className="rd-cancel-btn"
                    onClick={() => setCancelConfirm(true)}
                  >
                    Cancel request
                  </Button>
                ) : (
                  <div className="rd-confirm">
                    <p className="rd-confirm-text">Are you sure you want to cancel this request?</p>
                    {cancelError && (
                      <div className="rd-cancel-error" role="alert">
                        <AlertCircle size={14} aria-hidden="true" /> {cancelError}
                      </div>
                    )}
                    <div className="rd-confirm-actions">
                      <Button variant="danger" disabled={cancelling} onClick={handleCancel}>
                        {cancelling ? 'Cancelling…' : 'Yes, cancel'}
                      </Button>
                      <Button variant="secondary" onClick={() => setCancelConfirm(false)}>
                        Keep request
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
