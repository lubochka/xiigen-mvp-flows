import { chromium } from 'playwright';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';

const baseUrl = process.env.XIIGEN_FLOW28_BASE_URL ?? 'http://127.0.0.1:4328';
const cascadePoint = process.env.XIIGEN_CASCADE_POINT ?? 'C0-source-flow';
const moduleName = process.env.XIIGEN_FLOW_MODULE_NAME ?? 'blog-cms-modules';
const outputRoot = path.resolve(process.cwd(), '..', '..', '..', '..', 'docs', 'e2e-snapshots', 'blog-cms-modules', 'dotnet-angular', cascadePoint);

const roles = [
  { id: 'anonymous', fileId: 'anonymous', selector: '[data-testid="role-anonymous-blog-cms-modules"]' },
  { id: 'public_marketplace_visitor', fileId: 'public-marketplace-visitor', selector: '[data-testid="role-public-marketplace-visitor-blog-cms-modules"]' },
  { id: 'tenant_user', fileId: 'tenant-user', selector: '[data-testid="role-tenant-user-blog-cms-modules"]' },
  { id: 'tenant_admin', fileId: 'tenant-admin', selector: '[data-testid="role-tenant-admin-blog-cms-modules"]' },
  { id: 'freelancer', fileId: 'freelancer', selector: '[data-testid="role-freelancer-blog-cms-modules"]' },
  { id: 'business_partner', fileId: 'business-partner', selector: '[data-testid="role-business-partner-blog-cms-modules"]' },
  { id: 'event_organiser', fileId: 'event-organiser', selector: '[data-testid="role-event-organiser-blog-cms-modules"]' },
  { id: 'platform_admin', fileId: 'platform-admin', selector: '[data-testid="role-platform-admin-blog-cms-modules"]' },
  { id: 'platform_support', fileId: 'platform-support', selector: '[data-testid="role-platform-support-blog-cms-modules"]' }
];

const cells = [
  {
    id: 'C2-en-populated-desktop',
    state: 'reader',
    lang: 'en',
    viewport: { width: 1440, height: 1000 },
    pageSelector: '[data-testid="reader-page"]'
  },
  {
    id: 'C4-he-rtl-populated-desktop',
    state: 'author',
    lang: 'he',
    viewport: { width: 1440, height: 1000 },
    pageSelector: '[data-testid="author-page"]'
  },
  {
    id: 'C6-en-populated-mobile',
    state: 'operations',
    lang: 'en',
    viewport: { width: 390, height: 980 },
    pageSelector: '[data-testid="operations-page"]'
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
  JSON.stringify({ flow: 'FLOW-28', slug: 'blog-cms-modules', cascadePoint, moduleName, expected: roles.length * cells.length, actual: evidence.length, evidence }, null, 2)
);

