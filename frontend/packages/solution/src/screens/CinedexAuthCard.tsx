import type { FC, PropsWithChildren } from 'react';
import { AuthCard, type AuthCardProps } from '@cinedex/compounds';
import { Brand } from '../Brand/Brand';

export type CinedexAuthCardProps = AuthCardProps;

/**
 * `AuthCard` with the Cinedex brand already in it. Internal to this package —
 * every screen but `HomeScreen` uses this without passing `brand`, so none of
 * them repeats `brand={<Brand />}`. `HomeScreen` overrides it with an animated
 * intro, since it's the app's one landing moment.
 */
export const CinedexAuthCard: FC<PropsWithChildren<CinedexAuthCardProps>> = ({
  brand = <Brand />,
  ...props
}) => {
  return <AuthCard brand={brand} {...props} />;
};
