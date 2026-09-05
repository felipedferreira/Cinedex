import type { FC, ComponentProps } from 'react';
import { VisuallyHidden as VisuallyHiddenPrimitive } from 'radix-ui';

export type VisuallyHiddenProps = ComponentProps<
  typeof VisuallyHiddenPrimitive.Root
>;

/**
 * Content available to screen readers but not painted — for a heading or a
 * status message that the visual design carries some other way.
 */
export const VisuallyHidden: FC<VisuallyHiddenProps> = (props) => {
  return <VisuallyHiddenPrimitive.Root {...props} />;
};
