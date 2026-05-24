import { chromium } from 'playwright';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';

const baseUrl = process.env.XIIGEN_FLOW24_BASE_URL ?? 'http://127.0.0.1:4322';
const cascadePoint = process.env.XIIGEN_CASCADE_POINT ?? 'C0-source-flow';
const moduleName = process.env.XIIGEN_FLOW_MODULE_NAME ?? 'ai-safety-moderation';
const outputRoot = path.resolve(process.cwd(), '..', '..', '..', '..', 'docs', 'e2e-snapshots', 'ai-safety-moderation', 'dotnet-angular', cascadePoint);

const roles = [
  { id: 'anonymous', fileId: 'anonymous', selector: '[data-testid="role-anonymous-ai-safety-moderation"]' },
  { id: 'student', fileId: 'student', selector: '[data-testid="role-student-ai-safety-moderation"]' },
  { id: 'parent_guardian', fileId: 'parent-guardian', selector: '[data-testid="role-parent-guardian-ai-safety-moderation"]' },
  { id: 'educator', fileId: 'educator', selector: '[data-testid="role-educator-ai-safety-moderation"]' },
  { id: 'safety_reviewer', fileId: 'safety-reviewer', selector: '[data-testid="role-safety-reviewer-ai-safety-moderation"]' },
  { id: 'content_author', fileId: 'content-author', selector: '[data-testid="role-content-author-ai-safety-moderation"]' },
  { id: 'tenant_admin', fileId: 'tenant-admin', selector: '[data-testid="role-tenant-admin-ai-safety-moderation"]' },
  { id: 'platform_admin', fileId: 'platform-admin', selector: '[data-testid="role-platform-admin-ai-safety-moderation"]' }
];

const cells = [
  {
    id: 'C2-en-populated-desktop',
    state: 'consent',
    lang: 'en',
    viewport: { width: 1440, height: 1000 },
    pageSelector: '[data-testid="consent-page"]'
  },
  {
    id: 'C4-he-rtl-populated-desktop',
    state: 'lesson',
    lang: 'he',
    viewport: { width: 1440, height: 1000 },
    pageSelector: '[data-testid="lesson-page"]'
  },
  {
    id: 'C6-en-populated-mobile',
    state: 'progress',
    lang: 'en',
    viewport: { width: 390, height: 980 },
    pageSelector: '[data-testid="progress-page"]'
  }
];

await mkdir(outputRoot, { recursive: true });
const browser = await chromium.launch({ headless: true });
const evidence = [];

try {
  for (const role of roles) {
    for (const cell of cells) {
      const page = await browser.newPage({ viewport: cell.viewport });
      const url = `${baseUrl}/?role=${role.id}&state=${cell.state}&lang=${cell.lang}&module=${encodeURIComponent(moduleName)}`;
      await page.goto(url, { waitUntil: 'networkidle' });
      await page.locator(role.selector).waitFor({ state: 'visible', timeout: 10000 });
      await page.locator(cell.pageSelector).waitFor({ state: 'visible', timeout: 10000 });
      const fileName = `${role.fileId}-${cell.id}.png`;
      const filePath = path.join(outputRoot, fileName);
      await page.screenshot({ path: filePath, fullPage: true });
      evidence.push({ role: role.id, cell: cell.id, path: filePath, url });
      await page.close();
    }
  }
} finally {
  await browser.close();
}

await writeFile(
  path.join(outputRoot, 'visual-evidence-manifest.json'),
  JSON.stringify({ flow: 'FLOW-24', slug: 'ai-safety-moderation', cascadePoint, moduleName, expected: roles.length * cells.length, actual: evidence.length, evidence }, null, 2)
);

