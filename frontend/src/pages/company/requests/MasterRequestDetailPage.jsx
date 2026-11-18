import { useState, useEffect } from 'react';
import { useNavigate, Navigate } from 'react-router-dom';
import { motion, AnimatePresence, useReducedMotion } from 'motion/react';
import { ArrowLeft, AlertCircle, CheckCircle2 } from 'lucide-react';
import { Button, Card, PageTransition, Pill } from '../../../components/ui';
import { PlanRadioGroup } from './PlanCard';
import { formatAmount, formatDuration } from './planUtils';
import { apiGet, apiPost } from '../../../lib/api';
import { useNumericIdParam } from '../../../lib/useNumericIdParam';
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

function ApproveSkeleton() {
  return (
    <div className="mrd-approve-skeleton" aria-hidden="true">
      {[0, 1, 2].map((i) => <div key={i} className="mrd-approve-skel-row" />)}
    </div>
  );
}

export default function MasterRequestDetailPage() {
  const id = useNumericIdParam();
  const navigate = useNavigate();
  const reduce = useReducedMotion();

  const [req, setReq] = useState(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(null);

  // Reject
  const [rejectOpen, setRejectOpen] = useState(false);
  const [rejectComment, setRejectComment] = useState('');
  const [rejecting, setRejecting] = useState(false);
  const [rejectError, setRejectError] = useState(null);

  // Approve / plan picker
  const [approvePanelOpen, setApprovePanelOpen] = useState(false);
  const [plans, setPlans] = useState(null);
  const [plansLoading, setPlansLoading] = useState(false);
  const [plansError, setPlansError] = useState(false);
  const [selectedPlan, setSelectedPlan] = useState(null);
  const [approving, setApproving] = useState(false);
  const [approveError, setApproveError] = useState(null);
  const [approveSuccess, setApproveSuccess] = useState(null);

  useEffect(() => {
    if (id === null) return;
    let alive = true;
    setLoading(true);
    apiGet(`/api/master/subscription-requests/${id}`)
      .then((data) => {
        if (!alive) return;
        setReq(data);
        setLoading(false);
        if (data.status !== 'Pending') return;
        setPlansLoading(true);
        apiGet(`/api/master/subscription-requests/${id}/plans`)
          .then((plansData) => {
            if (!alive) return;
            setPlans(plansData.plans);
            setPlansLoading(false);
          })
          .catch(() => {
            if (alive) {
              setPlansError(true);
              setPlansLoading(false);
            }
          });
      })
      .catch((err) => { if (alive) { setLoadError(err.status === 404 ? 'not_found' : 'error'); setLoading(false); } });
    return () => { alive = false; };
  }, [id]);

  function handleRetryPlans() {
    setPlansLoading(true);
    setPlansError(false);
    apiGet(`/api/master/subscription-requests/${id}/plans`)
      .then((data) => { setPlans(data.plans); setPlansLoading(false); })
      .catch(() => { setPlansError(true); setPlansLoading(false); });
  }

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

  async function handleApprove() {
    if (!selectedPlan) return;
    setApproving(true);
    setApproveError(null);
    try {
      const result = await apiPost(
        `/api/master/subscription-requests/${id}/approve`,
        { planId: selectedPlan.subscriptionId },
      );
      setReq((r) => ({ ...r, status: 'Approved' }));
      const success = { subscriptionId: result.memberSubscriptionId, number: result.number, expiryDate: null };
      setApproveSuccess(success);
      apiGet(`/api/master/subscriptions/${result.memberSubscriptionId}`)
        .then((sub) => setApproveSuccess((s) => s ? { ...s, expiryDate: sub.expiryDate } : s))
        .catch(() => {});
    } catch (err) {
      if (err.status === 409 && err.code === 'TypeMismatch') {
        setApproveError('The selected plan type does not match this request.');
      } else if (err.status === 409) {
        setApproveError('This request is no longer pending.');
        setReq((r) => r ? { ...r, status: 'Processed' } : r);
      } else if (err.status === 404) {
        setApproveError('Request or plan not found.');
      } else {
        setApproveError('Something went wrong. Please try again.');
      }
    } finally {
      setApproving(false);
    }
  }

  if (id === null) return <Navigate to="/company/requests" replace />;

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
          {/* ── Main column ── */}
          <div className="mrd-main">
            <Card title="Request details">
              <div className="mrd-details">
                <DetailRow label="Request no."       value={req.requestNumber} />
                <DetailRow label="Requested on"      value={formatDate(req.requestedDateTime)} />
                <DetailRow label="Member"            value={req.memberName} />
                <DetailRow label="Member email"      value={req.memberEmail} />
                <DetailRow label="Company"           value={req.companyName} />
                <DetailRow label="TIN"               value={req.companyTin} />
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

          {/* ── Right column — only while pending or showing success ── */}
          {(isPending || approveSuccess) && (
            <aside>
              {/* Reject card */}
              {isPending && (
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
              )}

              {/* Approve card */}
              <Card title="Approve request" style={isPending ? { marginTop: 20 } : undefined}>
                {approveSuccess ? (
                  <div className="mrd-approve-success">
                    <p className="mrd-approve-success-title">
                      <CheckCircle2 size={18} className="mrd-approve-success-icon" aria-hidden="true" />
                      Request approved
                    </p>
                    <p className="mrd-approve-success-sub">
                      Subscription <strong>{approveSuccess.number}</strong> created.
                      {approveSuccess.expiryDate && (
                        <> Expires <strong>{formatDate(approveSuccess.expiryDate)}</strong>.</>
                      )}
                    </p>
                    <Button onClick={() => navigate(`/company/subscriptions/${approveSuccess.subscriptionId}`)}>
                      View subscription
                    </Button>
                  </div>
                ) : (
                  <>
                    <p className="mrd-action-info">
                      Assign a subscription plan to approve this request.
                    </p>

                    <AnimatePresence mode="wait">
                      {!approvePanelOpen ? (
                        <motion.div
                          key="btn"
                          initial={{ opacity: 1 }}
                          exit={reduce ? { opacity: 0 } : { opacity: 0, y: -4 }}
                          transition={{ duration: reduce ? 0.1 : 0.14, ease: 'easeIn' }}
                        >
                          <Button
                            variant="secondary"
                            onClick={() => setApprovePanelOpen(true)}
                          >
                            Select a plan
                          </Button>
                        </motion.div>
                      ) : (
                        <motion.div
                          key="plans"
                          initial={reduce ? { opacity: 0 } : { opacity: 0, y: 4 }}
                          animate={{ opacity: 1, y: 0 }}
                          transition={reduce ? { duration: 0.1 } : { duration: 0.2, ease: 'easeOut' }}
                        >
                          <div className="mrd-approve-body">
                            <div className="mrd-type-chip">
                              {req.subscriptionType} plans (requested by member)
                            </div>

                            {plansLoading && <ApproveSkeleton />}

                            {plansError && (
                              <div className="mrd-approve-err" role="alert">
                                <div className="mrd-approve-err-row">
                                  <AlertCircle size={14} aria-hidden="true" />
                                  Could not load plans.
                                </div>
                                <Button variant="secondary" onClick={handleRetryPlans}>Retry</Button>
                              </div>
                            )}

                            {!plansLoading && !plansError && plans !== null && plans.length === 0 && (
                              <div className="mrd-approve-empty">
                                No active {req.subscriptionType} plans. Add a plan before approving.
                              </div>
                            )}

                            {!plansLoading && !plansError && plans !== null && plans.length > 0 && (
                              <PlanRadioGroup
                                plans={plans}
                                selectedPlan={selectedPlan}
                                onSelect={setSelectedPlan}
                                compact
                              />
                            )}

                            {selectedPlan && (
                              <div className="mrd-approve-summary">
                                <div className="mrd-approve-summary-text">
                                  <strong>{selectedPlan.subscriptionName}</strong>
                                  {' — '}
                                  {formatDuration(selectedPlan.noOfActiveDuration, selectedPlan.durationType)},{' '}
                                  {formatAmount(selectedPlan.amount)}
                                  {selectedPlan.amount > 0 && (
                                    <div className="mrd-approve-summary-note">
                                      Payment is not collected here. The subscription activates immediately.
                                    </div>
                                  )}
                                </div>
                                {approveError && (
                                  <div className="mrd-error" role="alert">
                                    <AlertCircle size={14} aria-hidden="true" /> {approveError}
                                  </div>
                                )}
                                <div className="mrd-approve-actions">
                                  <Button disabled={approving} onClick={handleApprove}>
                                    {approving ? 'Approving…' : 'Approve request'}
                                  </Button>
                                  <button
                                    type="button"
                                    className="mrd-approve-clear"
                                    onClick={() => {
                                      setSelectedPlan(null);
                                      setApproveError(null);
                                      setApprovePanelOpen(false);
                                    }}
                                  >
                                    Clear
                                  </button>
                                </div>
                              </div>
                            )}
                          </div>
                        </motion.div>
                      )}
                    </AnimatePresence>
                  </>
                )}
              </Card>
            </aside>
          )}
        </div>
      </div>
    </PageTransition>
  );
}
