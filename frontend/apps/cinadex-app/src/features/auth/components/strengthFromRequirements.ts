export interface PasswordRequirement {
  label: string;
  met: boolean;
}

export function strengthFromRequirements(requirements: PasswordRequirement[]) {
  const metCount = requirements.filter((r) => r.met).length;
  const ratio = requirements.length === 0 ? 0 : metCount / requirements.length;
  if (ratio === 0) return { ratio: 0, label: 'Too weak' };
  if (ratio < 1) return { ratio, label: 'Weak' };
  return { ratio: 1, label: 'Strong' };
}
