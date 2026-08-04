---
name: Serene Professionalism
colors:
  surface: '#fbf9f3'
  surface-dim: '#dbdad4'
  surface-bright: '#fbf9f3'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f5f4ee'
  surface-container: '#efeee8'
  surface-container-high: '#e9e8e2'
  surface-container-highest: '#e3e3dd'
  on-surface: '#1b1c19'
  on-surface-variant: '#404847'
  inverse-surface: '#30312d'
  inverse-on-surface: '#f2f1eb'
  outline: '#717977'
  outline-variant: '#c0c8c6'
  surface-tint: '#3b6660'
  primary: '#25514b'
  on-primary: '#ffffff'
  primary-container: '#3e6963'
  on-primary-container: '#b9e7df'
  inverse-primary: '#a2cfc8'
  secondary: '#785921'
  on-secondary: '#ffffff'
  secondary-container: '#fed490'
  on-secondary-container: '#785a22'
  tertiary: '#275338'
  on-tertiary: '#ffffff'
  tertiary-container: '#3f6b4f'
  on-tertiary-container: '#b9e9c6'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#beece4'
  primary-fixed-dim: '#a2cfc8'
  on-primary-fixed: '#00201d'
  on-primary-fixed-variant: '#224e48'
  secondary-fixed: '#ffdeab'
  secondary-fixed-dim: '#e9c07e'
  on-secondary-fixed: '#271900'
  on-secondary-fixed-variant: '#5d420b'
  tertiary-fixed: '#beeecb'
  tertiary-fixed-dim: '#a2d2b0'
  on-tertiary-fixed: '#002110'
  on-tertiary-fixed-variant: '#244f35'
  background: '#fbf9f3'
  on-background: '#1b1c19'
  surface-variant: '#e3e3dd'
  papel: '#EDECE6'
  superficie: '#F6F5F0'
  superficie-2: '#E4E3DC'
  linea: '#D4D3CA'
  tinta: '#2B302D'
  tinta-2: '#5F6763'
  tinta-3: '#8B928D'
  marca-tenue: '#DCE5E1'
  sobre-marca: '#F2F5F3'
  noche-papel: '#1D2220'
  noche-superficie: '#262C29'
  noche-linea: '#363D3A'
typography:
  display-hero:
    fontFamily: Bricolage Grotesque
    fontSize: 32px
    fontWeight: '700'
    lineHeight: '1.2'
    letterSpacing: -0.02em
  headline-lg:
    fontFamily: Bricolage Grotesque
    fontSize: 24px
    fontWeight: '600'
    lineHeight: '1.3'
  headline-sm:
    fontFamily: Bricolage Grotesque
    fontSize: 17px
    fontWeight: '600'
    lineHeight: '1.4'
  body-md:
    fontFamily: Instrument Sans
    fontSize: 15px
    fontWeight: '400'
    lineHeight: '1.6'
  body-sm:
    fontFamily: Instrument Sans
    fontSize: 13.6px
    fontWeight: '400'
    lineHeight: '1.5'
  label-caps:
    fontFamily: Instrument Sans
    fontSize: 11px
    fontWeight: '700'
    lineHeight: '1'
    letterSpacing: 0.06em
  data-mono:
    fontFamily: Spline Sans Mono
    fontSize: 13px
    fontWeight: '500'
    lineHeight: '1'
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  base: 4px
  xs: 0.25rem
  sm: 0.5rem
  md: 1rem
  lg: 1.5rem
  xl: 2.5rem
  gutter: 16px
  margin-mobile: 16px
  margin-desktop: 24px
---

## Brand & Style

The design system is anchored in the principle of **"Low Visual Consumption,"** a philosophy specifically engineered to mitigate digital fatigue for professionals operating in high-stakes environments (such as pharmaceutical or medical POS) for 8+ hours. The brand personality is empathetic, utilitarian, and sophisticated—balancing the warmth of a physical workspace with the precision of a modern technical tool.

The chosen style is **Modern Tactile Minimalism**. It eschews the harshness of digital-pure colors in favor of a "paper-and-ink" aesthetic. It utilizes subtle tonal layering and physical metaphors, most notably the "ticket" signature, to ground the digital experience in reality. The interface feels calm and dependable, evoking a sense of quiet focus rather than urgency.

- **Minimalism:** Heavy focus on whitespace and purposeful typography to reduce cognitive load.
- **Tactile:** Elements use subtle, diffuse shadows and refined contrast ratios to feel "pressable" and physical.
- **Professional:** A balanced, corporate-modern structure that prioritizes data density without clutter.

## Colors

The palette is strictly governed by the "no pure white, no pure black" rule. We use low-chroma, warm grays to minimize eye strain.

- **Primary (Petroleum):** Used exclusively for primary actions, focus indicators, and meaningful selections.
- **Neutral (Papel):** The foundation of the system, providing a warm, organic backdrop that feels like stationery rather than a screen.
- **Secondary (Ocre):** Reserved for highlighting critical financial data or totals.
- **Tertiary (Exito):** Functional color for positive states.

**Tema Noche (Night Mode):**
Transition to the dark theme must maintain the same "low consumption" logic. Instead of pure black, use deep charcoal-greens (`#1D2220`) and desaturated teals (`#6E9A92`) to ensure that the interface remains soft on the eyes in low-light environments. Contrast ratios are carefully tuned to be readable without being piercing.

## Typography

Typography is optimized for long-term legibility. **Bricolage Grotesque** provides a distinctive, slightly organic feel for high-level numbers and branding, while **Instrument Sans** serves as a neutral, highly legible workhorse for UI elements and body text.

**Key Principles:**
- **Tabular Numerals:** All numeric data must use `font-variant-numeric: tabular-nums` to ensure perfect vertical alignment in tables and tickets.
- **Weight over Size:** To maintain a compact UI without sacrificing hierarchy, use font weight (600/700) to differentiate information before increasing font size.
- **Line Height:** Generous line-heights (1.6 for body) are used to prevent "text crowding" during long reading sessions.
- **Self-Hosting:** All fonts must be self-hosted to ensure the PWA remains fully functional offline.

## Layout & Spacing

The system follows an **8px base grid** with a 4px sub-step for fine-grained internal component spacing.

**Layout Models:**
- **POS Layout:** A split-screen fixed/fluid hybrid. The ticket sidebar is fixed at 400px on the right, while the product search grid fills the remaining space.
- **Form Layout:** Centered, narrow-column (max 640px) to keep line lengths readable.
- **Dashboard Layout:** A responsive fluid grid that reflows from 3 columns on desktop to a single stack on mobile.

**Breakpoints:**
- **Mobile (<900px):** The ticket sidebar transforms into a bottom-sheet (hoja inferior). Page margins reduce to 16px.
- **Tablet/Desktop (>=900px):** Sidebars become visible; grid gaps expand to 24px.

## Elevation & Depth

Hierarchy is established primarily through **Tonal Layers** rather than dramatic shadows.

- **Level 0 (Papel):** The base background layer. Always the most desaturated and "sunken."
- **Level 1 (Superficie):** Cards, panels, and primary surfaces. These sit slightly "above" the paper.
- **Level 2 (Active/Floating):** Used for modals and active dropdowns. This level uses **Ambient Shadows**—highly diffused, low-opacity (6% in light mode) shadows that provide a soft lift without creating visual "noise."

**Refined Contrast:** 
Avoid deep shadows in dark mode; instead, use slightly lighter border colors (`--linea`) to define the edges of elevated containers. 
The "Ticket" element uses a unique signature depth: a subtle inner glow combined with a jagged-edge mask to simulate physical paper thickness.

## Shapes

The shape language is approachable and refined, using a medium roundedness to soften the "industrial" nature of the tool.

- **Primary Radius (`--radio`):** 10px. Applied to main cards, product buttons, and containers.
- **Secondary Radius (`--radio-s`):** 7px. Used for internal elements like buttons and inputs to maintain a nested visual harmony.
- **Pill Shape:** Used for badges and status indicators to differentiate them from actionable buttons.
- **Signature Detail:** The "Ticket" bottom edge features a 14px repeating dentated/jagged mask, creating a physical "torn paper" metaphor. This is strictly reserved for the POS ticket and the Login card footer.

## Components

**Buttons:**
- **Primary:** High-contrast (`--marca` background). Weighted 700. Labels must be verb-driven ("Confirm Sale" vs "OK").
- **Secondary:** Tactile but minimal. Ghost borders (`--linea`) with a subtle `--superficie-2` background on hover.

**The Ticket Signature:**
The most distinctive component. It features a dashed separator for sub-totals and a jagged bottom edge. Use `Spline Sans Mono` for the lot numbers and transaction IDs within the ticket to emphasize its "printed" nature.

**Inputs:**
Fields use a "sunken" appearance with the `--superficie-2` background. On focus, the background transitions to `--superficie` and the border thickens to 1.5px in `--marca`. Error states are handled inline with a `--peligro` border; avoid floating toasts for validation.

**Chips & Badges:**
Status indicators use desaturated background tints (`--exito-tenue`, `--aviso-tenue`) with high-contrast text. They are strictly pill-shaped to avoid confusion with interactive buttons.

**Tables:**
Designed for data density. Headers are tiny, bold, and uppercase. Rows use a full-width hover state (`--marca-tenue`) to help the eye track data across long horizontal spans.