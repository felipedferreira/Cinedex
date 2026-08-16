import { AuthCard, type AuthCardProps } from '@cinedex/compounds';

export type CinedexAuthCardProps = AuthCardProps;

/**
 * `AuthCard` wrapper for the auth screens. Internal to this package.
 */
export const CinedexAuthCard = (props: CinedexAuthCardProps) => (
  <AuthCard {...props} />
);
