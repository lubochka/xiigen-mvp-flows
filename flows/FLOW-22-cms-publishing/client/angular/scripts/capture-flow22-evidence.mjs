import { chromium } from 'playwright';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';

const baseUrl = process.env.XIIGEN_FLOW22_BASE_URL ?? 'http://127.0.0.1:4321';
const cascadePoint = process.env.XIIGEN_CASCADE_POINT ?? 'C0-source-flow';
const moduleName = process.env.XIIGEN_FLOW_MODULE_NAME ?? 'cms-publishing';
const outputRoot = path.resolve(process.cwd(), '..', '..', '..', '..', 'docs', 'e2e-snapshots', 'cms-publishing', 'dotnet-angular', cascadePoint);

const roles = [
  { id: 'anonymous', fileId: 'anonymous', selector: '[data-testid="role-anonymous-cms-publishing"]' },
  { id: 'tenant_user', fileId: 'tenant-user', selector: '[data-testid="role-tenant-user-cms-publishing"]' },
  { id: 'content_author', fileId: 'content-author', selector: '[data-testid="role-content-author-cms-publishing"]' },
  { id: 'content_editor', fileId: 'content-editor', selector: '[data-testid="role-content-editor-cms-publishing"]' },
  { id: 'legal_reviewer', fileId: 'legal-reviewer', selector: '[data-testid="role-legal-reviewer-cms-publishing"]' },
  { id: 'publisher', fileId: 'publisher', selector: '[data-testid="role-publisher-cms-publishing"]' },
  { id: 'tenant_admin', fileId: 'tenant-admin', selector: '[data-testid="role-tenant-admin-cms-publishing"]' },
  { id: 'platform_admin', fileId: 'platform-admin', selector: '[data-testid="role-platform-admin-cms-publishing"]' }
];

const cells = [
  {
    id: 'C2-en-populated-desktop',
    state: 'drafts',
    lang: 'en',
    viewport: { width: 1440, height: 1000 },
    pageSelector: '[data-testid="drafts-page"]'
  },
  {
    id: 'C4-he-rtl-populated-desktop',
    state: 'approvals',
    lang: 'he',
    viewport: { width: 1440, height: 1000 },
    pageSelector: '[data-testid="approvals-page"]'
  },
  {
    id: 'C6-en-populated-mobile',
    state: 'analytics',
    lang: 'en',
    viewport: { width: 390, height: 980 },
    pageSelector: '[data-testid="analytics-page"]'
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
  JSON.stringify({ flow: 'FLOW-22', slug: 'cms-publishing', cascadePoint, moduleName, expected: roles.length * cells.length, actual: evidence.length, evidence }, null, 2)
);