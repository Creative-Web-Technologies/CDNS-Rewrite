// SectionLabel — the "LIVE PERFORMANCE / Workspace activity" row heading.
export default function SectionLabel({ eyebrow, title }) {
  return (
    <div className="dash-section">
      <span className="dash-section__eyebrow">{eyebrow}</span>
      <span className="dash-section__title">{title}</span>
    </div>
  );
}
