import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ArrowLeft, AlertCircle } from 'lucide-react';
import { Button, Card, PageTransition } from '../../../components/ui';
import { apiPost } from '../../../lib/api';
import './master-requests.css';

export default function MasterNewRequestPage() {
  const navigate = useNavigate();
  const [companyId, setCompanyId] = useState('');
  const [note, setNote] = useState('');
  const [submitStatus, setSubmitStatus] = useState('idle');
  const [submitError, setSubmitError] = useState(null);

  async function handleSubmit(e) {
    e.preventDefault();
    if (!companyId) return;
    const id = parseInt(companyId, 10);
    if (!id) return;
    setSubmitStatus('submitting');
    setSubmitError(null);
    try {
      const result = await apiPost('/api/master/subscription-requests', {
        companyId: id,
        note: note.trim() || null,
      });
      navigate(`/company/requests/${result.requestId}`);
    } catch (err) {
      setSubmitStatus('error');
      if (err.code === 'CompanyNotMapped') {
        setSubmitError('You are not an administrator of this company.');
      } else {
        setSubmitError('Something went wrong. Please try again.');
      }
    }
  }

  return (
    <PageTransition>
      <div className="mr-stack">
        <div className="mr-page-header">
          <div>
            <button type="button" className="mrd-back" onClick={() => navigate('/company/requests')}>
              <ArrowLeft size={16} aria-hidden="true" /> Back to requests
            </button>
          </div>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr', gap: 20 }}>
          <Card title="New Consignee request">
            <form onSubmit={handleSubmit} noValidate>
              <div className="mr-filter-field" style={{ marginBottom: 16 }}>
                <label htmlFor="nr-company" className="mr-label">Company ID</label>
                <input
                  id="nr-company"
                  type="number"
                  className="mr-input"
                  placeholder="e.g. 1"
                  value={companyId}
                  onChange={(e) => setCompanyId(e.target.value)}
                  min="1"
                  required
                />
              </div>
              <div className="mr-filter-field" style={{ marginBottom: 16 }}>
                <label htmlFor="nr-note" className="mr-label">Note (optional)</label>
                <textarea
                  id="nr-note"
                  className="mrd-textarea"
                  rows={3}
                  placeholder="Any notes…"
                  value={note}
                  onChange={(e) => setNote(e.target.value)}
                />
              </div>

              {submitError && (
                <div className="mrd-error" role="alert" style={{ marginBottom: 12 }}>
                  <AlertCircle size={14} aria-hidden="true" /> {submitError}
                </div>
              )}

              <div className="mr-filter-actions">
                <Button type="submit" disabled={!companyId || submitStatus === 'submitting'}>
                  {submitStatus === 'submitting' ? 'Submitting…' : 'Create request'}
                </Button>
                <Button type="button" variant="secondary" onClick={() => navigate('/company/requests')}>
                  Cancel
                </Button>
              </div>
            </form>
          </Card>

          <Card title="What this does">
            <ol className="nr-steps" style={{ margin: 0, paddingLeft: 20 }}>
              <li style={{ marginBottom: 10, fontSize: 14, color: 'var(--muted)' }}>
                Creates a Consignee subscription request for one of your companies.
              </li>
              <li style={{ marginBottom: 10, fontSize: 14, color: 'var(--muted)' }}>
                The request will be in Pending status and can be approved by any master of that company.
              </li>
              <li style={{ fontSize: 14, color: 'var(--muted)' }}>
                On the next screen you can review and approve the request to assign a plan.
              </li>
            </ol>
          </Card>
        </div>
      </div>
    </PageTransition>
  );
}
