export interface PasswordRequirement {
  label: string;
  met: boolean;
}

/**
 * Collapses a requirement checklist into a meter ratio and a one-word verdict.
 * Anything short of every requirement is "Weak" — there is no partial credit
 * beyond the bar's own fill.
 */
export function strengthFromRequirements(requirements: PasswordRequirement[]) {
  const metCount = requirements.filter((r) => r.met).length;
  const ratio = requirements.length === 0 ? 0 : metCount / requirements.length;
  if (ratio === 0) return { ratio: 0, label: 'Too weak' };
  if (ratio < 1) return { ratio, label: 'Weak' };
  return { ratio: 1, label: 'Strong' };
}
