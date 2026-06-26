import type { PebblePackage } from "@/api/queries"

// discountAmount is a fraction (0.1 = 10% off). Derive the display label and
// the pre-discount price the discount is applied against.
export function discountLabel(pkg: PebblePackage): string | null {
  return pkg.discountAmount > 0 ? `${Math.round(pkg.discountAmount * 100)}% off` : null
}

export function originalPrice(pkg: PebblePackage): number | null {
  return pkg.discountAmount > 0 ? pkg.dollarPrice / (1 - pkg.discountAmount) : null
}
