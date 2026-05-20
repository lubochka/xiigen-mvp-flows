import { chromium } from 'playwright';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';

const baseUrl = process.env.XIIGEN_FLOW09_BASE_URL ?? 'http://127.0.0.1:4309';
const cascadePoint = process.env.XIIGEN_CASCADE_POINT ?? 'C0-source-flow';
const moduleName = process.env.XIIGEN_FLOW_MODULE_NAME ?? 'transactional-event-participation';
const outputRoot = path.resolve(process.cwd(), '..', '..', '..', '..', 'docs', 'e2e-snapshots', 'transactional-event-participation', 'dotnet-angular', cascadePoint);

const roles = [
  { id: 'anonymous', fileId: 'anonymous', selector: '[data-testid="role-anonymous-transactional-event-participation"]' },
  { id: 'tenant_user', fileId: 'tenant-user', selector: '[data-testid="role-tenant-user-transactional-event-participation"]' },
  { id: 'referral_user', fileId: 'referral-user', selector: '[data-testid="role-referral-user-transactional-event-participation"]' },
  { id: 'freelancer', fileId: 'freelancer', selector: '[data-testid="role-freelancer-transactional-event-participation"]' },
  { id: 'business_partner', fileId: 'business-partner', selector: '[data-testid="role-business-partner-transactional-event-participation"]' },
  { id: 'event_organiser', fileId: 'event-organiser', selector: '[data-testid="role-event-organiser-transactional-event-participation"]' },
  { id: 'tenant_admin', fileId: 'tenant-admin', selector: '[data-testid="role-tenant-admin-transactional-event-participation"]' },
  { id: 'platform_admin', fileId: 'platform-admin', selector: '[data-testid="role-platform-admin-transactional-event-participation"]' },
  { id: 'platform_support', fileId: 'platform-support', selector: '[data-testid="role-platform-support-transactional-event-participation"]' }
];

const cells = [
  {
    id: 'C2-en-populated-desktop',
    state: 'purchase',
    lang: 'en',
    viewport: { width: 1440, height: 1000 },
    pageSelector: '[data-testid="page-transactional-event-participation-purchase"]'
  },
  {
    id: 'C4-he-rtl-populated-desktop',
    state: 'ticket',
    lang: 'he',
    viewport: { width: 1440, height: 1000 },
    pageSelector: '[data-testid="page-transactional-event-participation-ticket"]'
  },
  {
    id: 'C6-en-populated-mobile',
    state: 'refund',
    lang: 'en',
    viewport: { width: 390, height: 980 },
    pageSelector: '[data-testid="page-transactional-event-participation-refund"]'
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
console.log(`FLOW-09 visual evidence captured: ${evidence.length} PNGs at ${outputRoot}`);
