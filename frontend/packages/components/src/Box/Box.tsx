import type { ComponentPropsWithoutRef, ElementType, ReactNode } from 'react';
import { cx } from '../utils/cx';
import styles from './Box.module.css';

/** Steps on the spacing scale defined in `tokens.css`. */
export type Space = 0 | 1 | 2 | 3 | 4 | 5;

export type BoxDirection = 'row' | 'column';
export type BoxAlign = 'start' | 'center' | 'end' | 'stretch';
export type BoxJustify = 'start' | 'center' | 'end' | 'between';

const alignClass: Record<BoxAlign, string> = {
  start: styles.alignStart,
  center: styles.alignCenter,
  end: styles.alignEnd,
  stretch: styles.alignStretch,
};

const justifyClass: Record<BoxJustify, string> = {
  start: styles.justifyStart,
  center: styles.justifyCenter,
  end: styles.justifyEnd,
  between: styles.justifyBetween,
};

const paddingClass: Record<Space, string> = {
  0: styles.p0,
  1: styles.p1,
  2: styles.p2,
  3: styles.p3,
  4: styles.p4,
  5: styles.p5,
};

const gapClass: Record<Space, string> = {
  0: styles.g0,
  1: styles.g1,
  2: styles.g2,
  3: styles.g3,
  4: styles.g4,
  5: styles.g5,
};

interface BoxOwnProps {
  /** Element to render. Defaults to `div`. */
  as?: ElementType;
  direction?: BoxDirection;
  padding?: Space;
  gap?: Space;
  align?: BoxAlign;
  justify?: BoxJustify;
  className?: string;
  children?: ReactNode;
}

export type BoxProps = BoxOwnProps &
  Omit<ComponentPropsWithoutRef<'div'>, keyof BoxOwnProps>;

/**
 * The layout primitive: a flex container with padding and gap drawn from the
 * spacing tokens. Use `as` to change the rendered element without losing the
 * layout props.
 */
export function Box({
  as,
  direction = 'column',
  padding = 0,
  gap = 0,
  align,
  justify,
  className,
  children,
  ...rest
}: BoxProps) {
  const Component = as ?? 'div';

  return (
    <Component
      className={cx(
        styles.box,
        direction === 'row' ? styles.row : styles.column,
        paddingClass[padding],
        gapClass[gap],
        align && alignClass[align],
        justify && justifyClass[justify],
        className,
      )}
      {...rest}
    >
      {children}
    </Component>
  );
}
