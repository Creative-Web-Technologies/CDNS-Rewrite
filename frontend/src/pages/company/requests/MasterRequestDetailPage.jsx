import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, AlertCircle } from 'lucide-react';
import { Button, Card, PageTransition, Pill } from '../../../components/ui';
import { apiGet, apiPost } from '../../../lib/api';
import './master-request-detail.css';


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
    <div className="mrd-row">
      <span className="mrd-label">{label}</span>
      <span className="mrd-value">{value ?? '—'}</span>
    </div>
  );
}

export default function MasterRequestDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [req, setReq] = useState(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(null);

  // Reject
  const [rejectOpen, setRejectOpen] = useState(false);
  const [rejectComment, setRejectComment] = useState('');
  const [rejecting, setRejecting] = useState(false);
  const [rejectError, setRejectError] = useState(null);

  useEffect(() => {
    let alive = true;
    setLoading(true);
    apiGet(`/api/master/subscription-requests/${id}`)
      .then((data) => { if (alive) { setReq(data); setLoading(false); } })
      .catch((err) => { if (alive) { setLoadError(err.status === 404 ? 'not_found' : 'error'); setLoading(false); } });
    return () => { alive = false; };
  }, [id]);

  async function handleReject() {
    if (!rejectComment.trim()) return;
    setRejecting(true);
    setRejectError(null);
    try {
      await apiPost(`/api/master/subscription-requests/${id}/reject`, { comment: rejectComment.trim() });
      setReq((r) => ({ ...r, status: 'Rejected', responderComment: rejectComment.trim() }));
      setRejectOpen(false);
      setRejectComment('');
    } catch (err) {
      if (err.status === 409) {
        setRejectError('This request is no longer pending.');
        setReq((r) => r ? { ...r, status: 'Processed' } : r);
      } else if (err.status === 404) {
        setRejectError('Request not found.');
      } else {
        setRejectError('Something went wrong. Please try again.');
      }
    } finally {
      setRejecting(false);
    }
  }

  if (loading) {
    return (
      <PageTransition>
        <div className="mrd-loading" aria-busy="true">Loading…</div>
      </PageTransition>
    );
  }

  if (loadError === 'not_found' || (!loading && !req)) {
    return (
      <PageTransition>
        <div className="mrd-stack">
          <button type="button" className="mrd-back" onClick={() => navigate('/company/requests')}>
            <ArrowLeft size={16} aria-hidden="true" /> Back to requests
          </button>
          <Card>
            <div className="mrd-not-found">
              <p>Request not found or you do not have permission to view it.</p>
              <Button variant="secondary" onClick={() => navigate('/company/requests')}>Back to requests</Button>
            </div>
          </Card>
        </div>
      </PageTransition>
    );
  }

  const isPending = req?.status === 'Pending';

  return (
    <PageTransition>
      <div className="mrd-stack">
        <div className="mrd-top">
          <button type="button" className="mrd-back" onClick={() => navigate('/company/requests')}>
            <ArrowLeft size={16} aria-hidden="true" /> Back to requests
          </button>
        </div>

        <div className="mrd-header">
          <div>
            <h1 className="mrd-title">Request {req.requestNumber ?? `#${req.subscriptionRequestId}`}</h1>
            <p className="mrd-subtitle">Member subscription request details.</p>
          </div>
          <Pill tone={statusTone(req.status)}>{req.status}</Pill>
        </div>

        <div className="mrd-layout">
          <div className="mrd-main">
            <Card title="Request details">
              <div className="mrd-details">
                <DetailRow label="Request no." value={req.requestNumber} />
                <DetailRow label="Requested on" value={formatDate(req.requestedDateTime)} />
                <DetailRow label="Member" value={req.memberName} />
                <DetailRow label="Member email" value={req.memberEmail} />
                <DetailRow label="Company" value={req.companyName} />
                <DetailRow label="TIN" value={req.companyTin} />
                <DetailRow label="Subscription type" value={req.subscriptionType} />
                {req.note && <DetailRow label="Member note" value={req.note} />}
                {req.status !== 'Pending' && req.responderName && (
                  <DetailRow label="Responder" value={req.responderName} />
                )}
                {req.responderComment && (
                  <DetailRow label="Responder comment" value={req.responderComment} />
                )}
                {req.respondDateTime && (
                  <DetailRow label="Responded on" value={formatDate(req.respondDateTime)} />
                )}
              </div>
            </Card>
          </div>

          {isPending && (
            <aside className="mrd-side">
              {/* Reject */}
              <Card title="Reject request">
                <p className="mrd-action-info">
                  Rejecting notifies the member with a reason.
                </p>
                {!rejectOpen ? (
                  <Button
                    variant="secondary"
                    className="mrd-reject-btn"
                    onClick={() => setRejectOpen(true)}
                  >
                    Reject…
                  </Button>
                ) : (
                  <div>
                    <div className="mrd-field">
                      <label htmlFor="mrd-reject-comment" className="mrd-field-label">
                        Reason (required)
                      </label>
                      <textarea
                        id="mrd-reject-comment"
                        className="mrd-textarea"
                        rows={3}
                        placeholder="State the reason for rejection…"
                        value={rejectComment}
                        onChange={(e) => setRejectComment(e.target.value)}
                      />
                    </div>
                    {rejectError && (
                      <div className="mrd-error" role="alert">
                        <AlertCircle size={14} aria-hidden="true" /> {rejectError}
                      </div>
                    )}
                    <div className="mrd-actions">
                      <Button
                        variant="danger"
                        disabled={!rejectComment.trim() || rejecting}
                        onClick={handleReject}
                      >
                        {rejecting ? 'Rejecting…' : 'Confirm rejection'}
                      </Button>
                      <Button variant="secondary" onClick={() => { setRejectOpen(false); setRejectError(null); }}>
                        Cancel
                      </Button>
                    </div>
                  </div>
                )}
              </Card>

              {/* Approve */}
              <Card title="Approve request" style={{ marginTop: 20 }}>
                <p className="mrd-action-info">
                  Assign a subscription plan to approve this request.
                </p>
                <Button
                  onClick={() =>
                    navigate(`/company/requests/${id}/plans`, {
                      state: { type: req.subscriptionType },
                    })
                  }
                >
                  Pick a plan…
                </Button>
              </Card>
            </aside>
          )}
        </div>
      </div>
    </PageTransition>
  );
}
