import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Search, CheckCircle2, AlertCircle, ArrowLeft } from 'lucide-react';
import { Button, Card, PageTransition } from '../../../components/ui';
import { apiGet, apiPost } from '../../../lib/api';
import './new-request.css';

export default function NewRequestPage() {
  const navigate = useNavigate();

  const [tin, setTin] = useState('');
  const [lookupStatus, setLookupStatus] = useState('idle');
  const [foundCompany, setFoundCompany] = useState(null);
  const [subType, setSubType] = useState(null);
  const [note, setNote] = useState('');
  const [submitStatus, setSubmitStatus] = useState('idle');
  const [submitError, setSubmitError] = useState(null);

  async function handleLookup() {
    if (!tin.trim()) return;
    setLookupStatus('loading');
    setFoundCompany(null);
    setSubType(null);
    try {
      const company = await apiGet(`/api/member/companies/lookup?tin=${encodeURIComponent(tin.trim())}`);
      setFoundCompany(company);
      setLookupStatus('found');
    } catch (err) {
      setLookupStatus(err.code === 'CompanyNotFound' ? 'notfound' : 'error');
    }
  }

  async function handleSubmit(e) {
    e.preventDefault();
    if (!foundCompany || !subType) return;
    setSubmitStatus('submitting');
    setSubmitError(null);
    try {
      const result = await apiPost('/api/member/subscription-requests', {
        companyId: foundCompany.companyId,
        subscriptionType: subType,
        note: note.trim() || undefined,
      });
      navigate(`/member/requests/${result.requestId}`);
    } catch (err) {
      setSubmitStatus('error');
      if (err.code === 'DuplicatePendingRequest') {
        setSubmitError('You already have a pending request for this company and subscription type.');
      } else if (err.code === 'CompanyNotFound') {
        setSubmitError('Company not found. Please look up the company again.');
      } else {
        setSubmitError('Something went wrong. Please try again.');
      }
    }
  }

  const canSubmit = foundCompany && subType && submitStatus !== 'submitting';

  return (
    <PageTransition>
      <div className="nr-stack">
        <div className="nr-top">
          <button type="button" className="nr-back" onClick={() => navigate('/member/requests')}>
            <ArrowLeft size={16} aria-hidden="true" /> Back to requests
          </button>
        </div>

        <div className="nr-layout">
          <div className="nr-main">
            <Card title="Request a subscription">
              <form className="nr-form" onSubmit={handleSubmit} noValidate>
                <div className="nr-field">
                  <label htmlFor="nr-tin" className="nr-label">Company TIN (9 digits)</label>
                  <div className="nr-tin-row">
                    <input
                      id="nr-tin"
                      type="text"
                      className="nr-input"
                      placeholder="123456789"
                      maxLength={12}
                      value={tin}
                      onChange={(e) => { setTin(e.target.value); setLookupStatus('idle'); setFoundCompany(null); }}
                      onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); handleLookup(); } }}
                      aria-describedby="nr-lookup-msg"
                    />
                    <Button
                      type="button"
                      variant="secondary"
                      icon={Search}
                      disabled={!tin.trim() || lookupStatus === 'loading'}
                      onClick={handleLookup}
                    >
                      Look up company
                    </Button>
                  </div>

                  {lookupStatus === 'loading' && (
                    <p id="nr-lookup-msg" className="nr-hint">Looking up…</p>
                  )}
                  {lookupStatus === 'found' && foundCompany && (
                    <div id="nr-lookup-msg" className="nr-found" role="status">
                      <CheckCircle2 size={16} aria-hidden="true" />
                      <span><strong>{foundCompany.name}</strong> found for TIN {foundCompany.tin}</span>
                    </div>
                  )}
                  {lookupStatus === 'notfound' && (
                    <p id="nr-lookup-msg" className="nr-error-msg" role="alert">
                      No company found for TIN {tin.trim()}.
                    </p>
                  )}
                  {lookupStatus === 'error' && (
                    <p id="nr-lookup-msg" className="nr-error-msg" role="alert">
                      Couldn't look up the company. Please try again.
                    </p>
                  )}
                </div>

                {foundCompany && (
                  <fieldset className="nr-fieldset">
                    <legend className="nr-label">Subscription type</legend>
                    <div className="nr-type-cards">
                      <label className={`nr-type-card${subType === 1 ? ' is-selected' : ''}`}>
                        <input
                          type="radio"
                          name="subType"
                          value="1"
                          className="nr-radio-hidden"
                          checked={subType === 1}
                          onChange={() => setSubType(1)}
                        />
                        <span className="nr-type-card__title">Declarant</span>
                        <span className="nr-type-card__desc">Usually a clearing agent (CHA) lodging declarations</span>
                      </label>
                      <label className={`nr-type-card${subType === 2 ? ' is-selected' : ''}`}>
                        <input
                          type="radio"
                          name="subType"
                          value="2"
                          className="nr-radio-hidden"
                          checked={subType === 2}
                          onChange={() => setSubType(2)}
                        />
                        <span className="nr-type-card__title">Consignee</span>
                        <span className="nr-type-card__desc">Usually the importer receiving the goods</span>
                      </label>
                    </div>
                  </fieldset>
                )}

                {foundCompany && (
                  <div className="nr-field">
                    <label htmlFor="nr-note" className="nr-label">Note to the company (optional)</label>
                    <textarea
                      id="nr-note"
                      className="nr-textarea"
                      rows={3}
                      placeholder="Any additional information for the company…"
                      value={note}
                      onChange={(e) => setNote(e.target.value)}
                    />
                  </div>
                )}

                {submitError && (
                  <div className="nr-submit-error" role="alert">
                    <AlertCircle size={16} aria-hidden="true" />
                    <span>{submitError}</span>
                  </div>
                )}

                {foundCompany && (
                  <div className="nr-form-actions">
                    <Button type="submit" disabled={!canSubmit}>
                      {submitStatus === 'submitting' ? 'Sending…' : 'Send request'}
                    </Button>
                    <Button type="button" variant="secondary" onClick={() => navigate('/member/requests')}>
                      Cancel
                    </Button>
                  </div>
                )}
              </form>
            </Card>
          </div>

          <aside className="nr-side">
            <Card title="What happens next">
              <ol className="nr-steps">
                <li className="nr-step">
                  <span className="nr-step__num">1</span>
                  <span>Enter the company TIN and confirm the company name.</span>
                </li>
                <li className="nr-step">
                  <span className="nr-step__num">2</span>
                  <span>Choose Declarant or Consignee.</span>
                </li>
                <li className="nr-step">
                  <span className="nr-step__num">3</span>
                  <span>Every member of that company is notified in the app.</span>
                </li>
                <li className="nr-step">
                  <span className="nr-step__num">4</span>
                  <span>The company master assigns a plan and activates it, or rejects with a comment.</span>
                </li>
              </ol>
            </Card>
          </aside>
        </div>
      </div>
    </PageTransition>
  );
}
