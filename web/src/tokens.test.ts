import { describe, expect, it } from 'vitest';
import tokens from '../../.agentic/ux/design-tokens.json';
import css from './styles/tokens.css?raw';

function luminance(hex: string): number {
  const [r, g, b] = [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16) / 255).map((c) => (c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4));
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

function contrast(a: string, b: string): number {
  const [hi, lo] = [luminance(a), luminance(b)].sort((x, y) => y - x);
  return (hi + 0.05) / (lo + 0.05);
}

const kebab = (s: string) => s.replace(/[A-Z]/g, (c) => `-${c.toLowerCase()}`);

describe('design tokens', () => {
  it('every text/background pair the UI uses meets WCAG AA (4.5:1)', () => {
    const c = tokens.color as Record<string, string>;
    const pairs: [string, string][] = [
      ['text', 'bg'], ['muted', 'bg'], ['muted', 'surface'], ['muted', 'surfaceSunken'], ['accent', 'bg'], ['accent', 'surface'],
      ['onAccent', 'accent'], ['accentStrong', 'accentSoft'], ['onInk', 'ink'], ['danger', 'surface'], ['success', 'surface'],
    ];
    for (const [fg, bg] of pairs) {
      expect(contrast(c[fg], c[bg]), `${fg} on ${bg}`).toBeGreaterThanOrEqual(4.5);
    }
  });

  it('tokens.css matches design-tokens.json so code and design cannot drift', () => {
    for (const [name, value] of Object.entries(tokens.color)) {
      expect(css).toContain(`--color-${kebab(name)}: ${value};`);
    }
    for (const [name, value] of Object.entries(tokens.space)) {
      expect(css).toContain(`--space-${name}: ${value};`);
    }
    for (const [name, value] of Object.entries(tokens.radius)) {
      expect(css).toContain(`--radius-${name}: ${value};`);
    }
  });
});
