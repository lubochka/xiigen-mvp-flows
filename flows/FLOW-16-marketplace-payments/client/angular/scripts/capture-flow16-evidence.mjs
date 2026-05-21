import { chromium } from 'playwright';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';

const baseUrl = process.env.XIIGEN_FLOW16_BASE_URL ?? 'http://127.0.0.1:4316';
const cascadePoint = process.env.XIIGEN_CASCADE_POINT ?? 'C0-source-flow';
const moduleName = process.env.XIIGEN_FLOW_MODULE_NAME ?? 'marketplace-payments';
const outputRoot = path.resolve(process.cwd(), '..', '..', '..', '..', 'docs', 'e2e-snapshots', 'marketplace-payments', 'dotnet-angular', cascadePoint);

const roles = [
  { id: 'anonymous', fileId: 'anonymous', selector: '[data-testid="role-anonymous-marketplace-payments"]' },
  { id: 'public_visitor', fileId: 'public-visitor', selector: '[data-testid="role-public-visitor-marketplace-payments"]' },
  { id: 'tenant_user', fileId: 'tenant-user', selector: '[data-testid="role-tenant-user-marketplace-payments"]' },
  { id: 'tenant_admin', fileId: 'tenant-admin', selector: '[data-testid="role-tenant-admin-marketplace-payments"]' },
  { id: 'referral_user', fileId: 'referral-user', selector: '[data-testid="role-referral-user-marketplace-payments"]' },
  { id: 'freelancer', fileId: 'freelancer', selector: '[data-testid="role-freelancer-marketplace-payments"]' },
  { id: 'business_partner', fileId: 'business-partner', selector: '[data-testid="role-business-partner-marketplace-payments"]' },
  { id: 'event_organiser', fileId: 'event-organiser', selector: '[data-testid="role-event-organiser-marketplace-payments"]' },
  { id: 'platform_admin', fileId: 'platform-admin', selector: '[data-testid="role-platform-admin-marketplace-payments"]' },
  { id: 'platform_support', fileId: 'platform-support', selector: '[data-testid="role-platform-support-marketplace-payments"]' }
];

const cells = [
  {
    id: 'C2-en-populated-desktop',
    state: 'checkout',
    lang: 'en',
    viewport: { width: 1440, height: 1000 },
    pageSelector: '[data-testid="checkout-page"]'
  },
  {
    id: 'C4-he-rtl-populated-desktop',
    state: 'escrow',
    lang: 'he',
    viewport: { width: 1440, height: 1000 },
    pageSelector: '[data-testid="escrow-dashboard"]'
  },
  {
    id: 'C6-en-populated-mobile',
    state: 'admin',
    lang: 'en',
    viewport: { width: 390, height: 980 },
    pageSelector: '[data-testid="refund-queue"]'
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
console.log(`FLOW-16 visual evidence captured: ${evidence.length} PNGs at ${outputRoot}`);
