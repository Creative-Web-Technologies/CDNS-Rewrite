// Maps error codes from the CusDec API to user-visible messages.

export const GATE_CODES = new Set([
  'SubscriptionNotFound',
  'SubscriptionInactive',
  'SubscriptionExpired',
  'CompanyInactive',
  'SubscriptionKeyInvalid',
]);

export function isGateError(code) {
  return GATE_CODES.has(code);
}

const GATE_MESSAGES = {
  SubscriptionInactive:  { title: 'Subscription inactive',    message: 'This subscription is currently inactive. Contact the company administrator.' },
  SubscriptionExpired:   { title: 'Subscription expired',     message: 'This subscription has expired. Renew it to continue using the workspace.' },
  CompanyInactive:       { title: 'Company account inactive', message: 'The company account associated with this subscription is inactive.' },
  SubscriptionKeyInvalid:{ title: 'Invalid subscription key', message: 'The subscription key is not valid. Contact the company administrator.' },
};

export function gateBlockedInfo(code) {
  return GATE_MESSAGES[code] ?? { title: 'Access denied', message: 'You do not have access to this subscription.' };
}

const CUSDEC_MESSAGES = {
  CusDecNotFound:    'This declaration could not be found.',
  AlreadyDisclaimed: 'A colleague already disclaimed this declaration.',
  AlreadyClaimed:    'A colleague already acknowledged this declaration.',
  NotClaimed:        'This declaration has not been acknowledged yet.',
};

export function cusDecErrorMessage(code) {
  return CUSDEC_MESSAGES[code] ?? 'Something went wrong. Please try again.';
}
