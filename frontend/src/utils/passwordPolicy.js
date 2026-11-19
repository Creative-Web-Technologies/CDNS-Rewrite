// Mirrors server PasswordPolicy: 8-128 chars, upper, lower, digit, special
export const PASSWORD_RULES = [
  { key: 'minLength',  label: '8 to 128 characters',           test: v => v.length >= 8 && v.length <= 128 },
  { key: 'hasUpper',   label: 'One uppercase letter (A–Z)',     test: v => /[A-Z]/.test(v) },
  { key: 'hasLower',   label: 'One lowercase letter (a–z)',     test: v => /[a-z]/.test(v) },
  { key: 'hasDigit',   label: 'One number (0–9)',               test: v => /\d/.test(v) },
  { key: 'hasSpecial', label: 'One special character (!@#$…)',  test: v => /[^A-Za-z0-9]/.test(v) },
];

export function checkPassword(value) {
  const checks = {};
  PASSWORD_RULES.forEach(r => { checks[r.key] = r.test(value); });
  return { ...checks, valid: PASSWORD_RULES.every(r => checks[r.key]) };
}
