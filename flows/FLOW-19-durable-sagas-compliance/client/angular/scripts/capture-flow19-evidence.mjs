import { chromium } from 'playwright';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';

const baseUrl = process.env.XIIGEN_FLOW19_BASE_URL ?? 'http://127.0.0.1:4318';
const cascadePoint = process.env.XIIGEN_CASCADE_POINT ?? 'C0-source-flow';
const moduleName = process.env.XIIGEN_FLOW_MODULE_NAME ?? 'durable-sagas-compliance';
const outputRoot = path.resolve(process.cwd(), '..', '..', '..', '..', 'docs', 'e2e-snapshots', 'durable-sagas-compliance', 'dotnet-angular', cascadePoint);

const roles = [
  { id: 'anonymous', fileId: 'anonymous', selector: '[data-testid="role-anonymous-durable-sagas-compliance"]' },
  { id: 'tenant_user', fileId: 'tenant-user', selector: '[data-testid="role-tenant-user-durable-sagas-compliance"]' },
  { id: 'tenant_admin', fileId: 'tenant-admin', selector: '[data-testid="role-tenant-admin-durable-sagas-compliance"]' },
  { id: 'tenant_consumer', fileId: 'tenant-consumer', selector: '[data-testid="role-tenant-consumer-durable-sagas-compliance"]' },
  { id: 'tenant_ops', fileId: 'tenant-ops', selector: '[data-testid="role-tenant-ops-durable-sagas-compliance"]' },
  { id: 'saga_operator', fileId: 'saga-operator', selector: '[data-testid="role-saga-operator-durable-sagas-compliance"]' },
  { id: 'compliance_officer', fileId: 'compliance-officer', selector: '[data-testid="role-compliance-officer-durable-sagas-compliance"]' },
  { id: 'platform_ops', fileId: 'platform-ops', selector: '[data-testid="role-platform-ops-durable-sagas-compliance"]' },
  { id: 'platform_admin', fileId: 'platform-admin', selector: '[data-testid="role-platform-admin-durable-sagas-compliance"]' },
  { id: 'platform_support', fileId: 'platform-support', selector: '[data-testid="role-platform-support-durable-sagas-compliance"]' }
];

const cells = [
  {
    id: 'C2-en-populated-desktop',
    state: 'operations',
    lang: 'en',
    viewport: { width: 1440, height: 1000 },
    pageSelector: '[data-testid="saga-operations-page"]'
  },
  {
    id: 'C4-he-rtl-populated-desktop',
    state: 'sagas',
    lang: 'he',
    viewport: { width: 1440, height: 1000 },
    pageSelector: '[data-testid="saga-dashboard-page"]'
  },
  {
    id: 'C6-en-populated-mobile',
    state: 'compliance',
    lang: 'en',
    viewport: { width: 390, height: 980 },
    pageSelector: '[data-testid="compliance-audit-page"]'
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

await writeFile(path.join(outputRoot, 'visual-evidence-manifest.json'), JSON.stringify({ cascadePoint, moduleName, count: evidence.length, evidence }, null, 2));
console.log(`FLOW-19 visual evidence captured: ${evidence.length} PNGs at ${outputRoot}`);
