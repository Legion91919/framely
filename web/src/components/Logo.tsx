export function Logo({ size = 28 }: { size?: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 28 28" aria-hidden="true">
      <path d="M3 9V3h6M19 3h6v6M25 19v6h-6M9 25H3v-6" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" />
      <circle cx="14" cy="14" r="4" fill="var(--color-accent)" />
    </svg>
  );
}
