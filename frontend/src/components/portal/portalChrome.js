import { createContext, useContext, useEffect } from 'react';

// Lets a page inject chrome into the shared PortalLayout header (its actions
// slot, and optionally an override title/subtitle). The layout owns the state;
// pages register on mount and clear on unmount.
export const PortalChromeContext = createContext({ setChrome: () => {} });

export function usePortalChrome(chrome) {
  const { setChrome } = useContext(PortalChromeContext);
  useEffect(() => {
    setChrome(chrome ?? {});
    return () => setChrome({});
    // Pages pass a stable description; re-run only if the setter identity changes.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [setChrome]);
}
