import { chromium } from 'playwright';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';

const baseUrl = process.env.XIIGEN_FLOW27_BASE_URL ?? 'http://127.0.0.1:4327';
const cascadePoint = process.env.XIIGEN_CASCADE_POINT ?? 'C0-source-flow';
const moduleName = process.env.XIIGEN_FLOW_MODULE_NAME ?? 'human-interaction-gate';
const outputRoot = path.resolve(process.cwd(), '..', '..', '..', '..', 'docs', 'e2e-snapshots', 'human-interaction-gate', 'dotnet-angular', cascadePoint);

const roles = [
  { id: 'anonymous', fileId: 'anonymous', selector: '[data-testid="role-anonymous-human-interaction-gate"]' },
  { id: 'tenant_user', fileId: 'tenant-user', selector: '[data-testid="role-tenant-user-human-interaction-gate"]' },
  { id: 'tenant_admin', fileId: 'tenant-admin', selector: '[data-testid="role-tenant-admin-human-interaction-gate"]' },
  { id: 'platform_admin', fileId: 'platform-admin', selector: '[data-testid="role-platform-admin-human-interaction-gate"]' },
  { id: 'platform_support', fileId: 'platform-support', selector: '[data-testid="role-platform-support-human-interaction-gate"]' },
  { id: 'human_decision_maker', fileId: 'human-decision-maker', selector: '[data-testid="role-human-decision-maker-human-interaction-gate"]' }
];

const cells = [
  {
    id: 'C2-en-populated-desktop',
    state: 'queue',
    lang: 'en',
    viewport: { width: 1440, height: 1000 },
    pageSelector: '[data-testid="queue-page"]'
  },
  {
    id: 'C4-he-rtl-populated-desktop',
    state: 'claims',
    lang: 'he',
    viewport: { width: 1440, height: 1000 },
    pageSelector: '[data-testid="claims-page"]'
  },
  {
    id: 'C6-en-populated-mobile',
    state: 'completion',
    lang: 'en',
    viewport: { width: 390, height: 980 },
    pageSelector: '[data-testid="completion-page"]'
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
  JSON.stringify({ flow: 'FLOW-27', slug: 'human-interaction-gate', cascadePoint, moduleName, expected: roles.length * cells.length, actual: evidence.length, evidence }, null, 2)
);
