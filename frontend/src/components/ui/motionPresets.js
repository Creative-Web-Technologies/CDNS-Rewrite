// Motion variants shared by the portals. We only animate two things:
// page-enter (the content area when a route mounts) and card-enter (cards/tiles
// fading up). prefers-reduced-motion is respected via the `reduced` helpers,
// which components pick using motion's useReducedMotion hook.

export const pageEnter = {
  initial: { opacity: 0, y: 8 },
  animate: { opacity: 1, y: 0 },
  transition: { duration: 0.28, ease: [0.16, 1, 0.3, 1] },
};

export const cardEnter = {
  initial: { opacity: 0, y: 10 },
  animate: { opacity: 1, y: 0 },
  transition: { duration: 0.32, ease: [0.16, 1, 0.3, 1] },
};

// Collapsed variants used when the user prefers reduced motion: no transform,
// no fade — content simply appears.
export const noMotion = {
  initial: false,
  animate: { opacity: 1, y: 0 },
  transition: { duration: 0 },
};
