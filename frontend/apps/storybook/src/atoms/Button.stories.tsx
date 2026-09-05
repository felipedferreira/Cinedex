import type { Meta, StoryObj } from '@storybook/react-vite';
import { Button } from '@cinedex/atoms';

const meta = {
  title: 'Atoms/Button',
  component: Button,
  tags: ['autodocs'],
  args: {
    children: 'Count is 0',
    variant: 'primary',
    size: 'md',
  },
  argTypes: {
    variant: {
      control: 'inline-radio',
      options: ['primary', 'solid', 'outline'],
    },
    size: { control: 'inline-radio', options: ['md', 'block'] },
    onClick: { action: 'clicked' },
  },
} satisfies Meta<typeof Button>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Primary: Story = {};

/** The auth flow's call to action: `solid` at `block`. */
export const SolidBlock: Story = {
  args: { variant: 'solid', size: 'block', children: 'Sign in' },
};

export const OutlineBlock: Story = {
  args: { variant: 'outline', size: 'block', children: 'Send again' },
};

export const Disabled: Story = {
  args: { disabled: true, children: 'Unavailable' },
};

/**
 * `asChild` renders the caller's element with the button's styling — how a
 * router `<Link>` becomes a call to action without nesting an `<a>` in a
 * `<button>`.
 */
export const AsLink: Story = {
  args: {
    asChild: true,
    variant: 'outline',
    size: 'block',
    children: <a href="#reset">Reset password instead</a>,
  },
};
