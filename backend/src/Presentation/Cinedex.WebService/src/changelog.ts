import { marked } from "marked";
import "./style.css";

async function renderChangelog(target: HTMLElement): Promise<void> {
  try {
    const response = await fetch("CHANGELOG.md");
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`);
    }
    const markdown = await response.text();
    target.innerHTML = await marked.parse(markdown);
    target.removeAttribute("aria-busy");
  } catch (error) {
    target.textContent = `Failed to load changelog: ${(error as Error).message}`;
    target.removeAttribute("aria-busy");
  }
}

document.addEventListener("DOMContentLoaded", () => {
  const target = document.getElementById("changelog");
  if (target) {
    void renderChangelog(target);
  }
});
