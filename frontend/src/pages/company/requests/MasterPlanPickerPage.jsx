import { useState, useEffect } from 'react';
import { useNavigate, useLocation, Navigate } from 'react-router-dom';
import { ArrowLeft, AlertCircle } from 'lucide-react';
import { Button, Card, PageTransition } from '../../../components/ui';
import { usePortalChrome } from '../../../components/portal/portalChrome';
import { PlanCard } from './PlanCard';
import { formatAmount, formatDuration } from './planUtils';
import { apiGet, apiPost } from '../../../lib/api';
import { useNumericIdParam } from '../../../lib/useNumericIdParam';
import './master-plan-picker.css';

const TABS = [
  { key: 'Consignee', label: 'Importer/Exporter (Consignee)' },
  { key: 'Declarant', label: 'CHA Agent (Declarant)' },
];

function DetailRow({ label, value }) {
  return (
    <div className="mpp-detail-row">
      <span className="mpp-detail-label">{label}</span>
      <span className="mpp-detail-value">{value ?? '—'}</span>
    </div>
  );
}

function ConfirmDialog({ plan, onConfirm, onCancel }) {
  return (
    <div className="mpp-overlay" role="dialog" aria-modal="true">
      <div className="mpp-dialog">
        <p className="mpp-dialog-title">Confirm selection</p>
        <p className="mpp-dialog-body">
          You have selected a &ldquo;{plan.subscriptionType}&rdquo; type subscription.
          Click Yes if this is correct.
        </p>
        <div className="mpp-dialog-actions">
          <Button onClick={onConfirm}>Yes, continue</Button>
          <Button variant="secondary" onClick={onCancel}>Cancel</Button>
        </div>
      </div>
    </div>
  );
}

function ConfirmationScreen({ plan, requestId, onBack }) {
  const navigate = useNavigate();
  const [approving, setApproving] = useState(false);
  const [error, setError] = useState(null);

  async function handleConfirm() {
    setApproving(true);
    setError(null);
    try {
      const result = await apiPost(
        `/api/master/subscription-requests/${requestId}/approve`,
        { planId: plan.subscriptionId },
      );
      navigate(`/company/subscriptions/${result.memberSubscriptionId}`);
    } catch (err) {
      if (err.status === 409 && err.code === 'TypeMismatch') {
        setError('The selected plan type does not match this request. Please go back and choose a matching plan.');
      } else if (err.status === 409) {
        setError('This request is no longer pending.');
      } else if (err.status === 404) {
        setError('Request or plan not found.');
      } else {
        setError('Something went wrong. Please try again.');
      }
    } finally {
      setApproving(false);
    }
  }

  const isFree = plan.amount === 0;

  return (
    <div className="mpp-confirm-layout">
      <Card title="Subscription details">
        <div className="mpp-plan-detail">
          <DetailRow label="Plan name"      value={plan.subscriptionName} />
          <DetailRow label="Type"           value={plan.subscriptionType} />
          <DetailRow label="Duration"       value={formatDuration(plan.noOfActiveDuration, plan.durationType)} />
          <DetailRow label="Amount"         value={isFree ? 'FREE' : formatAmount(plan.amount)} />
          <DetailRow label="Applicable for" value={plan.actionType === 'AcknowledgementOnly' ? 'Only For Acknowledgement' : 'General'} />
        </div>
      </Card>

      <Card title="Payment">
        <p className="mpp-payment-stub">
          {isFree
            ? 'This is a free plan. No payment required.'
            : 'Online payment is not yet available. Please contact your administrator to arrange payment.'}
        </p>
        {error && (
          <div className="mpp-error" role="alert">
            <AlertCircle size={14} aria-hidden="true" /> {error}
          </div>
        )}
        <div style={{ marginTop: 16, display: 'flex', flexDirection: 'column', gap: 8 }}>
          <Button disabled={approving} onClick={handleConfirm}>
            {approving ? 'Approving…' : 'Confirm subscription'}
          </Button>
          <Button variant="secondary" onClick={onBack}>Back to plans</Button>
        </div>
      </Card>
    </div>
  );
}

export default function MasterPlanPickerPage() {
  const id = useNumericIdParam();
  const navigate = useNavigate();
  const location = useLocation();

  usePortalChrome({
    title: 'New subscription',
    subtitle: 'Choose a plan for your company.',
  });

  // Seed the tab from router state so it feels instant on normal navigation;
  // the useEffect below corrects it from the API once the response arrives.
  const stateType = location.state?.type ?? 'Consignee';
  const [activeTab, setActiveTab] = useState(stateType === 'Declarant' ? 'Declarant' : 'Consignee');

  const [plans, setPlans] = useState(null);
  const [isSelf, setIsSelf] = useState(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(null);

  const [pendingPlan, setPendingPlan] = useState(null);
  const [confirmedPlan, setConfirmedPlan] = useState(null);

  useEffect(() => {
    if (id === null) return;
    let alive = true;
    setLoading(true);
    apiGet(`/api/master/subscription-requests/${id}/plans`)
      .then((data) => {
        if (alive) {
          setPlans(data.plans);
          setIsSelf(data.isSelf);
          setActiveTab(data.requestType === 'Declarant' ? 'Declarant' : 'Consignee');
          setLoading(false);
        }
      })
      .catch(() => { if (alive) { setLoadError(true); setLoading(false); } });
    return () => { alive = false; };
  }, [id]);

  function handleSelect(plan) {
    setPendingPlan(plan);
  }

  function handleDialogConfirm() {
    setConfirmedPlan(pendingPlan);
    setPendingPlan(null);
  }

  if (id === null) return <Navigate to="/company/requests" replace />;

  // Redirect member requests to the detail page once the response confirms !isSelf.
  // While still loading, show the loading state so there is no flash of the picker.
  if (!loading && !loadError && isSelf === false) {
    return <Navigate replace to={`/company/requests/${id}`} />;
  }

  const tabPlans = plans?.filter((p) => p.subscriptionType === activeTab) ?? [];

  if (loading) {
    return (
      <PageTransition>
        <div className="mpp-loading" aria-busy="true">Loading plans…</div>
      </PageTransition>
    );
  }

  if (loadError) {
    return (
      <PageTransition>
        <div className="mpp-stack">
          <div className="mpp-top">
            <button type="button" className="mpp-back" onClick={() => navigate(-1)}>
              <ArrowLeft size={16} aria-hidden="true" /> Back
            </button>
          </div>
          <Card>
            <div className="mpp-empty">Could not load plans. Please try again.</div>
          </Card>
        </div>
      </PageTransition>
    );
  }

  return (
    <PageTransition>
      <div className="mpp-stack">
        <div className="mpp-top">
          <button type="button" className="mpp-back" onClick={() => navigate(-1)}>
            <ArrowLeft size={16} aria-hidden="true" /> Back
          </button>
        </div>

        {confirmedPlan ? (
          <ConfirmationScreen
            plan={confirmedPlan}
            requestId={id}
            onBack={() => setConfirmedPlan(null)}
          />
        ) : (
          <Card>
            <div className="mpp-tabs">
              {TABS.map((tab) => (
                <button
                  key={tab.key}
                  type="button"
                  className={`mpp-tab${activeTab === tab.key ? ' is-active' : ''}`}
                  onClick={() => setActiveTab(tab.key)}
                >
                  {tab.label}
                </button>
              ))}
            </div>

            {tabPlans.length === 0 ? (
              <div className="mpp-empty">No plans available for this type.</div>
            ) : (
              <div className="pc-grid mpp-grid-margin">
                {tabPlans.map((plan) => (
                  <PlanCard key={plan.subscriptionId} plan={plan} onSelect={handleSelect} />
                ))}
              </div>
            )}
          </Card>
        )}

        {pendingPlan && (
          <ConfirmDialog
            plan={pendingPlan}
            onConfirm={handleDialogConfirm}
            onCancel={() => setPendingPlan(null)}
          />
        )}
      </div>
    </PageTransition>
  );
}
