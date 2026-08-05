/**
 * Joins truthy class names with a space.
 *
 * Deliberately tiny — the library has no `clsx`/`classnames` dependency, and
 * conditional class selection is all any component here needs.
 */
export function cx(...classes: (string | false | null | undefined)[]): string {
  return classes.filter(Boolean).join(' ');
}
