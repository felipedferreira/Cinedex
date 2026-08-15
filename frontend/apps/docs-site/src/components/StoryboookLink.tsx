import useDocusaurusContext from '@docusaurus/useDocusaurusContext';

export default function StoryboookLink(): JSX.Element {
  const { siteConfig } = useDocusaurusContext();
  const storybookUrl = (siteConfig.customFields as { storybookBaseUrl: string }).storybookBaseUrl;

  return (
    <a href={storybookUrl} target="_blank" rel="noopener noreferrer">
      Storybook workbench
    </a>
  );
}
