import { checkPassword, PASSWORD_RULES } from '../../utils/passwordPolicy';
import './PasswordRequirements.css';

export default function PasswordRequirements({ value }) {
  const checks = checkPassword(value);
  return (
    <ul className="pw-requirements" aria-label="Password requirements">
      {PASSWORD_RULES.map(r => (
        <li key={r.key} className={checks[r.key] ? 'pw-req--met' : 'pw-req--unmet'}>
          <span className="pw-req-icon" aria-hidden="true">{checks[r.key] ? '✓' : '○'}</span>
          {r.label}
        </li>
      ))}
    </ul>
  );
}
