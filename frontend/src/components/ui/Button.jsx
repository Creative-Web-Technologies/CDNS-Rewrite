// Button — the single button primitive for both portals.
// Variants: primary (filled), secondary (outline), ghost (text), danger.
export default function Button({
  variant = 'primary',
  size = 'md',
  type = 'button',
  icon: Icon,
  iconRight: IconRight,
  className = '',
  children,
  ...rest
}) {
  const classes = [
    'ui-btn',
    `ui-btn--${variant}`,
    `ui-btn--${size}`,
    !children ? 'ui-btn--icon-only' : '',
    className,
  ].filter(Boolean).join(' ');

  return (
    <button type={type} className={classes} {...rest}>
      {Icon && <Icon className="ui-btn__icon" aria-hidden="true" />}
      {children && <span>{children}</span>}
      {IconRight && <IconRight className="ui-btn__icon" aria-hidden="true" />}
    </button>
  );
}
