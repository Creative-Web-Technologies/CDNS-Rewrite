import { useEffect, useRef } from 'react';

const SCRIPT_URL = 'https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit';

export default function TurnstileWidget({ onToken, onError }) {
  const containerRef = useRef(null);
  const onTokenRef = useRef(onToken);
  const onErrorRef = useRef(onError);

  useEffect(() => {
    onTokenRef.current = onToken;
    onErrorRef.current = onError;
  }, [onToken, onError]);

  useEffect(() => {
    // Cloudflare's public test key is only used for local development.
    const siteKey = import.meta.env.VITE_TURNSTILE_SITE_KEY
      || (import.meta.env.DEV ? '1x00000000000000000000AA' : '');
    if (!siteKey) {
      onErrorRef.current('Security verification is not configured. Contact your administrator.');
      return undefined;
    }

    let widgetId;
    let cancelled = false;
    const handleScriptError = () => {
      onErrorRef.current('Security verification could not load. Check your connection and try again.');
    };
    const render = () => {
      if (cancelled || !window.turnstile || !containerRef.current) return;
      widgetId = window.turnstile.render(containerRef.current, {
        sitekey: siteKey,
        callback: token => onTokenRef.current(token),
        'expired-callback': () => onTokenRef.current(''),
        'error-callback': () => onErrorRef.current('Verification could not be completed. Try again.'),
        theme: 'light',
      });
    };

    let script = document.querySelector(`script[src="${SCRIPT_URL}"]`);
    if (!script) {
      script = document.createElement('script');
      script.src = SCRIPT_URL;
      script.async = true;
      script.defer = true;
      document.head.appendChild(script);
    }
    script.addEventListener('load', render);
    script.addEventListener('error', handleScriptError);
    if (window.turnstile) render();

    return () => {
      cancelled = true;
      script.removeEventListener('load', render);
      script.removeEventListener('error', handleScriptError);
      if (widgetId !== undefined) window.turnstile?.remove(widgetId);
    };
  }, []);

  return <div ref={containerRef} className="turnstile-widget" aria-label="Security verification" />;
}
