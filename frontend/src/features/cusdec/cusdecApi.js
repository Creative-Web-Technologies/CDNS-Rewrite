import { apiGet, apiPost } from '../../lib/api';

export const getSubscriptions = () =>
  apiGet('/api/cusdec/subscriptions');

export const getStrip = (subId) =>
  apiGet(`/api/cusdec/${subId}`);

export const getCusDecs = (subId, params) =>
  apiGet(`/api/cusdec/${subId}/cusdecs?${params}`);

export const getOffices = (subId) =>
  apiGet(`/api/cusdec/${subId}/offices`);

export const acknowledge = (subId, id) =>
  apiPost(`/api/cusdec/${subId}/cusdecs/${id}/acknowledge`);

export const disclaim = (subId, id) =>
  apiPost(`/api/cusdec/${subId}/cusdecs/${id}/disclaim`);

export const complete = (subId, id) =>
  apiPost(`/api/cusdec/${subId}/cusdecs/${id}/complete`);

export const reopen = (subId, id) =>
  apiPost(`/api/cusdec/${subId}/cusdecs/${id}/reopen`);

export const getCusDecDetail = (subId, id) =>
  apiGet(`/api/cusdec/${subId}/cusdecs/${id}`);

export const getCusDecTimeline = (subId, id) =>
  apiGet(`/api/cusdec/${subId}/cusdecs/${id}/timeline`);
