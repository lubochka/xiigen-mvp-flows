import { chromium } from 'playwright';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';

const baseUrl = process.env.XIIGEN_FLOW13_BASE_URL ?? 'http://127.0.0.1:4313';
const cascadePoint = process.env.XIIGEN_CASCADE_POINT ?? 'C0-source-flow';
const moduleName = process.env.XIIGEN_FLOW_MODULE_NAME ?? 'data-warehouse-analytics';
const outputRoot = path.resolve(process.cwd(), '..', '..', '..', '..', 'docs', 'e2e-snapshots', 'data-warehouse-analytics', 'dotnet-angular', cascadePoint);

const roles = [
  { id: 'anonymous', fileId: 'anonymous', selector: '[data-testid="role-anonymous-data-warehouse-analytics"]' },
  { id: 'tenant_user', fileId: 'tenant-user', selector: '[data-testid="role-tenant-user-data-warehouse-analytics"]' },
  { id: 'referral_user', fileId: 'referral-user', selector: '[data-testid="role-referral-user-data-warehouse-analytics"]' },
  { id: 'freelancer', fileId: 'freelancer', selector: '[data-testid="role-freelancer-data-warehouse-analytics"]' },
  { id: 'business_partner', fileId: 'business-partner', selector: '[data-testid="role-business-partner-data-warehouse-analytics"]' },
  { id: 'event_organiser', fileId: 'event-organiser', selector: '[data-testid="role-event-organiser-data-warehouse-analytics"]' },
  { id: 'tenant_admin', fileId: 'tenant-admin', selector: '[data-testid="role-tenant-admin-data-warehouse-analytics"]' },
  { id: 'platform_admin', fileId: 'platform-admin', selector: '[data-testid="role-platform-admin-data-warehouse-analytics"]' },
  { id: 'platform_support', fileId: 'platform-support', selector: '[data-testid="role-platform-support-data-warehouse-analytics"]' }
];

const cells = [
  {
    id: 'C2-en-populated-desktop',
    state: 'plan',
    lang: 'en',
    viewport: { width: 1440, height: 1000 },
    pageSelector: '[data-testid="warehouse-plan-form"]'
  },
  {
    id: 'C4-he-rtl-populated-desktop',
    state: 'ingest',
    lang: 'he',
    viewport: { width: 1440, height: 1000 },
    pageSelector: '[data-testid="event-ingestion-page"]'
  },
  {
    id: 'C6-en-populated-mobile',
    state: 'dashboard',
    lang: 'en',
    viewport: { width: 390, height: 980 },
    pageSelector: '[data-testid="warehouse-dashboard-page"]'
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
console.log(`FLOW-13 visual evidence captured: ${evidence.length} PNGs at ${outputRoot}`);
