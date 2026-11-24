import { useState, useEffect, useRef, useCallback } from 'react';
import { getCusDecs } from './cusdecApi';

export function buildListParams(subId, tab, trackingState, filters, page, pageSize, subscriptionType) {
  const params = new URLSearchParams({ tab, page: String(page), pageSize: String(pageSize) });

  if (tab === 'tracking' && trackingState && trackingState !== 'all') {
    params.set('state', trackingState);
  }

  const search = filters.search?.trim();
  if (search) params.set('search', search);

  const cusDecNumber = filters.cusDecNumber?.trim();
  if (cusDecNumber) params.set('cusDecNumber', cusDecNumber);

  const tin = (filters.counterpartyTin ?? '').replace(/\s+/g, '');
  if (tin) {
    const tinKey = subscriptionType === 'Declarant' ? 'consigneeTin' : 'declarantTin';
    params.set(tinKey, tin);
  }

  if (filters.office) params.set('office', filters.office);
  if (filters.fromDate) params.set('from', filters.fromDate);
  if (filters.toDate) params.set('to', filters.toDate);

  if (filters.sort) {
    params.set('sort', filters.sort);
  }

  return params;
}

export function useCusDecList({ subId, tab, trackingState, filters, page, pageSize = 4, subscriptionType }) {
  const [items, setItems] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [containersBlocked, setContainersBlocked] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const reqIdRef = useRef(0);

  const load = useCallback(async () => {
    if (!subId) return;
    const reqId = ++reqIdRef.current;
    setLoading(true);
    setError(null);
    try {
      const params = buildListParams(subId, tab, trackingState, filters, page, pageSize, subscriptionType);
      const result = await getCusDecs(subId, params);
      if (reqId !== reqIdRef.current) return;
      setItems(result?.items ?? []);
      setTotalCount(result?.totalCount ?? 0);
      setContainersBlocked(result?.containersBlocked ?? false);
    } catch (err) {
      if (reqId !== reqIdRef.current) return;
      setError(err);
    } finally {
      if (reqId === reqIdRef.current) setLoading(false);
    }
  }, [subId, tab, trackingState, filters, page, pageSize, subscriptionType]);

  useEffect(() => { load(); }, [load]);

  return { items, setItems, totalCount, setTotalCount, containersBlocked, loading, error, refetch: load };
}
