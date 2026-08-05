import type { Meta, StoryObj } from '@storybook/react-vite';
import { Box } from './Box';
import { Button } from '../Button/Button';

const meta = {
  title: 'Primitives/Box',
  component: Box,
  tags: ['autodocs'],
  args: {
    direction: 'column',
    padding: 3,
    gap: 2,
  },
  argTypes: {
    direction: { control: 'inline-radio', options: ['row', 'column'] },
    padding: { control: 'inline-radio', options: [0, 1, 2, 3, 4, 5] },
    gap: { control: 'inline-radio', options: [0, 1, 2, 3, 4, 5] },
    align: {
      control: 'inline-radio',
      options: ['start', 'center', 'end', 'stretch'],
    },
    justify: {
      control: 'inline-radio',
      options: ['start', 'center', 'end', 'between'],
    },
  },
} satisfies Meta<typeof Box>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Column: Story = {
  args: {
    children: (
      <>
        <Button>First</Button>
        <Button variant="ghost">Second</Button>
        <Button variant="ghost">Third</Button>
      </>
    ),
  },
};

export const Row: Story = {
  args: {
    direction: 'row',
    align: 'center',
    children: (
      <>
        <Button>First</Button>
        <Button variant="ghost">Second</Button>
        <Button variant="ghost">Third</Button>
      </>
    ),
  },
};

export const SpaceBetween: Story = {
  args: {
    direction: 'row',
    justify: 'between',
    align: 'center',
    children: (
      <>
        <span>Total</span>
        <Button size="sm">Checkout</Button>
      </>
    ),
  },
};

export const AsSection: Story = {
  name: 'Rendered as <section>',
  args: {
    as: 'section',
    children: <p>Any element can carry the layout props via `as`.</p>,
  },
};
