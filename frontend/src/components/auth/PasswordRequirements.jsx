import { checkPassword, PASSWORD_RULES } from '../../utils/passwordPolicy';
import './PasswordRequirements.css';

const RULE_LABELS = {
  minLength:  '8+ characters',
  hasUpper:   'Uppercase letter',
  hasLower:   'Lowercase letter',
  hasDigit:   'Number',
  hasSpecial: 'Symbol',
};

export default function PasswordRequirements({ value }) {
  const checks = checkPassword(value || '');
  return (
    <div className="pw-rules-card" aria-label="Password requirements">
      {PASSWORD_RULES.map(r => {
        const met = checks[r.key];
        return (
          <div key={r.key} className={`pw-rule${met ? ' pw-rule--met' : ''}`}>
            <span className="pw-rule-circle" aria-hidden="true">
              {met && (
                <svg width="12" height="12" viewBox="0 0 12 12" fill="none">
                  <polyline
                    points="2,6.5 5,9.5 10,3"
                    stroke="#fff"
                    strokeWidth="1.5"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    className="pw-rule-check"
                  />
                </svg>
              )}
            </span>
            <span className="pw-rule-label">{RULE_LABELS[r.key] ?? r.label}</span>
          </div>
        );
      })}
    </div>
  );
}
