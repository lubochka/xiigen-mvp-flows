import { chromium } from 'playwright';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';

const baseUrl = process.env.XIIGEN_FLOW29_BASE_URL ?? 'http://127.0.0.1:4329';
const cascadePoint = process.env.XIIGEN_CASCADE_POINT ?? 'C0-source-flow';
const moduleName = process.env.XIIGEN_FLOW_MODULE_NAME ?? 'adaptive-rag-deep-research';
const outputRoot = path.resolve(process.cwd(), '..', '..', '..', '..', 'docs', 'e2e-snapshots', 'adaptive-rag-deep-research', 'dotnet-angular', cascadePoint);

const roles = [
  { id: 'anonymous', fileId: 'anonymous', selector: '[data-testid="role-anonymous-adaptive-rag-deep-research"]' },
  { id: 'public_marketplace_visitor', fileId: 'public-marketplace-visitor', selector: '[data-testid="role-public-marketplace-visitor-adaptive-rag-deep-research"]' },
  { id: 'tenant_user', fileId: 'tenant-user', selector: '[data-testid="role-tenant-user-adaptive-rag-deep-research"]' },
  { id: 'tenant_admin', fileId: 'tenant-admin', selector: '[data-testid="role-tenant-admin-adaptive-rag-deep-research"]' },
  { id: 'prompt_approver', fileId: 'prompt-approver', selector: '[data-testid="role-prompt-approver-adaptive-rag-deep-research"]' },
  { id: 'human_decision_maker', fileId: 'human-decision-maker', selector: '[data-testid="role-human-decision-maker-adaptive-rag-deep-research"]' },
  { id: 'agency_access', fileId: 'agency-access', selector: '[data-testid="role-agency-access-adaptive-rag-deep-research"]' },
  { id: 'platform_admin', fileId: 'platform-admin', selector: '[data-testid="role-platform-admin-adaptive-rag-deep-research"]' },
  { id: 'platform_support', fileId: 'platform-support', selector: '[data-testid="role-platform-support-adaptive-rag-deep-research"]' }
];

const cells = [
  {
    id: 'C2-en-populated-desktop',
    state: 'routing',
    lang: 'en',
    viewport: { width: 1440, height: 1000 },
    pageSelector: '[data-testid="routing-page"]'
  },
  {
    id: 'C4-he-rtl-populated-desktop',
    state: 'graph',
    lang: 'he',
    viewport: { width: 1440, height: 1000 },
    pageSelector: '[data-testid="graph-page"]'
  },
  {
    id: 'C6-en-populated-mobile',
    state: 'promotion',
    lang: 'en',
    viewport: { width: 390, height: 980 },
    pageSelector: '[data-testid="promotion-page"]'
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
  JSON.stringify({ flow: 'FLOW-29', slug: 'adaptive-rag-deep-research', cascadePoint, moduleName, expected: roles.length * cells.length, actual: evidence.length, evidence }, null, 2)
);

