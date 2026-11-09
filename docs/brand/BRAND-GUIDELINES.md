# FarmerSample.com — Brand Guidelines

**Version:** 0.1 (draft)  
**Positioning:** Verified at origin. Traded worldwide.  
**Business:** Sample-first B2B marketplace for graded, lab-tested Sri Lankan spices and dry goods, with auctions introduced as buyer activity grows.

![FarmerSample.com brand identity concept](a_clean_professional_brand_identity_board_for_far.png)

## Brand purpose

Connect verified Sri Lankan producers directly with international buyers through transparent product grading, laboratory reports, paid samples, secure transactions and export support. The identity should communicate **trust, traceability, quality and Sri Lankan origin**.

## Logo direction

- **Primary concept:** A Sri Lankan plantation landscape with recognizable spices and a premium wordmark.
- **Primary wordmark:** `FarmerSample.com` — *Farmer* in deep green and *Sample* in spice gold.
- **Recommended tagline:** **Verified at origin. Traded worldwide.**
- **Supporting descriptor:** Sri Lanka · Spices · Direct from Farmers.
- **Practical use:** Reserve the detailed illustrated emblem for hero sections, packaging and marketing. Commission a simplified vector icon and horizontal wordmark for website navigation, favicons and small labels.
- **Logo variants:** Full-color on cream; reversed gold/cream on deep green; single-color dark green.
- **Minimum clear space:** At least the height of the capital “F” around the mark (provisional; confirm once vector artwork is finalized).
- **Avoid:** Distortion, busy photographic backgrounds, low contrast, and the detailed landscape emblem at favicon sizes.

## Color palette

| Token | Color | HEX | Intended use |
|---|---|---|---|
| Primary / Ceylon Green | Deep forest green | `#0F3D2E` | Navigation, headings, primary brand surfaces |
| Accent / Spice Gold | Warm golden ochre | `#C8922F` | Auction actions, highlights, premium details |
| Secondary / Cinnamon | Warm terracotta | `#A64B2A` | Spice storytelling, category accents |
| Background / Tea Cream | Soft ivory | `#F7F3E8` | Page backgrounds, packaging |
| Supporting / Ocean Teal | Muted teal | `#2E7D7A` | Logistics, information, secondary accents |
| Text / Ink | Near-black green | `#14251E` | Body text |
| Surface / White | White | `#FFFFFF` | Cards and forms |

**Usage balance:** Approximately 60% light backgrounds, 25% green/ink, 10% gold and 5% supporting accents. Check WCAG contrast for all live UI combinations; gold is better as a fill or decorative accent than small text on cream.

## Typography

- **Display / headlines:** Playfair Display, weights 600–700. Use for editorial headings, hero statements and premium brand moments.
- **Interface / body:** Inter, weights 400–700. Use for product specifications, forms, navigation, prices, labels and calls to action.
- **Fallbacks:** Georgia for display; Arial or system sans-serif for interface.
- **Tone:** Confident, transparent, warm, precise and internationally credible. Avoid exaggerated claims about certification, provenance or farmer income unless substantiated.

## Website components

### Navigation

Cream or white background, compact horizontal wordmark, dark-green navigation labels, clear **Browse lots** and **Order samples** actions.

### Hero

Real photography of Sri Lankan farms and products, with a concise value proposition:

> **Discover verified spices, directly from origin.**  
> Explore graded Sri Lankan lots, review lab reports and order samples before you buy.

Primary CTA: **Explore lots**. Secondary CTA: **How verification works**.

### Lot cards

Display product name, farmer/producer, origin, grade, available volume, sample price, lab report status, transaction format (offer/auction), and a clear CTA. Distinguish independently verified claims from producer-supplied information.

### Trust signals

Use consistent simple line icons for **Verified producer**, **Graded & lab tested**, **Secure transaction**, and **Export supported**. Each badge must link to a clear explanation of what is actually checked and by whom.

### Photography and imagery

Prefer authentic photographs of Sri Lankan producers, pepper vines, cinnamon, cardamom, grading processes, sample kits and warehousing. Use warm natural light and restrained editing. Obtain permission for identifiable farmer portraits and avoid generic stock imagery presented as actual partners.

## Brand voice

- **Do:** “Review the lot’s grade and lab report before ordering a sample.”
- **Do:** “Meet the producer and understand the product’s origin.”
- **Avoid:** “Guaranteed highest farmer prices” or “100% risk-free” unless demonstrably true.

## Launch messaging

**Short description:** FarmerSample.com connects international buyers with graded, lab-tested spices and dry goods from Sri Lanka. Buyers can discover producers, order samples and make offers, with secure trade and export support.

**Longer-term story:** Launch the sample showroom in 2027, introduce live and online auctions as buyer liquidity develops, and explore expansion to Kerala, India, in year three. These are plans, not completed milestones.

## Starter CSS variables

```css
:root {
  --color-primary: #0F3D2E;
  --color-accent: #C8922F;
  --color-cinnamon: #A64B2A;
  --color-background: #F7F3E8;
  --color-teal: #2E7D7A;
  --color-text: #14251E;
  --color-surface: #FFFFFF;
  --font-display: 'Playfair Display', Georgia, serif;
  --font-body: Inter, Arial, sans-serif;
  --radius-card: 12px;
  --radius-button: 8px;
}
```

## Next design deliverables

1. Simplified, production-ready vector logo in SVG, including light/dark versions.
2. Square icon and favicon designed for 16–48 px readability.
3. Product card, lot detail page, sample-order flow and auction interface designs.
4. Sample box labels and a lab-report template using the same visual system.
5. Accessibility review and contrast-tested UI states.

---

*This is a visual identity proposal, not a certification or legal assurance. The name, logo, operational claims and launch assumptions remain subject to validation.*

## Logo files in this repository

| File | Use |
|------|-----|
| `docs/brand/logo/farmersample-emblem-original.png` | Original illustrated emblem as supplied (1536×1024, background is a baked-in checkerboard, not transparent) |
| `docs/brand/logo/farmersample-emblem-cutout.png` | Same emblem with the background removed. Use on **light** backgrounds only (cream/white); edges look rough on dark green |
| `web/projects/buyer-web/public/brand/emblem-*.webp` | Web versions (640 and 1200 px) used in the homepage hero |
| `docs/brand/logo/farmersample-icon.svg` | Square "FS" icon: favicon, app icon, small labels (also `apple-touch-icon.png`, 180 px) |
| `docs/brand/logo/farmersample-wordmark.svg` | Horizontal wordmark with descriptor line, as supplied |
| `docs/brand/logo/farmersample-wordmark-compact.svg` | Same without the descriptor line, for headers (~40 px high) where it would be unreadable |
| `docs/brand/logo/farmersample-wordmark-compact-reversed.svg` | Compact wordmark in cream and lighter gold for dark green backgrounds (footer, back-office sidebar) |

Still open: a true transparent version of the illustrated emblem for dark backgrounds, and converting the text in the SVG icon and wordmark to outlines (they use the viewer's Georgia/Arial fonts, so letter shapes can differ slightly between devices).
