import './ComingSoon.css';

export default function ComingSoon({ title }) {
  return (
    <div className="coming-soon">
      <svg className="coming-soon__icon" viewBox="0 0 64 64" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
        <circle cx="32" cy="32" r="30" stroke="#d1d5db" strokeWidth="2"/>
        <path d="M22 32h20M32 22v20" stroke="#9ca3af" strokeWidth="2.5" strokeLinecap="round"/>
      </svg>
      <h2 className="coming-soon__heading">{title}</h2>
      <p className="coming-soon__text">This section is under construction and will be available soon.</p>
    </div>
  );
}
