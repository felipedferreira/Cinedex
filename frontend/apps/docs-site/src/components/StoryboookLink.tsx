import type { FC } from 'react';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';

const StoryboookLink: FC = () => {
  const { siteConfig } = useDocusaurusContext();
  const storybookUrl = (siteConfig.customFields as { storybookBaseUrl: string })
    .storybookBaseUrl;

  return (
    <a href={storybookUrl} target="_blank" rel="noopener noreferrer">
      Storybook workbench
    </a>
  );
};

export default StoryboookLink;
