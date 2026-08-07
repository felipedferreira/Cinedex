import { AuthCard, type AuthCardProps } from '@cinedex/compounds';
import { Brand } from '../Brand/Brand';

export type CinedexAuthCardProps = Omit<AuthCardProps, 'brand'>;

/**
 * `AuthCard` with the Cinedex brand already in it. Internal to this package —
 * every screen uses it so none of them repeats `brand={<Brand />}`.
 */
export function CinedexAuthCard(props: CinedexAuthCardProps) {
  return <AuthCard brand={<Brand />} {...props} />;
}
