import { chromium } from 'playwright';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';

const baseUrl = process.env.XIIGEN_FLOW23_BASE_URL ?? 'http://127.0.0.1:4321';
const cascadePoint = process.env.XIIGEN_CASCADE_POINT ?? 'C0-source-flow';
const moduleName = process.env.XIIGEN_FLOW_MODULE_NAME ?? 'form-builder-templates';
const outputRoot = path.resolve(process.cwd(), '..', '..', '..', '..', 'docs', 'e2e-snapshots', 'form-builder-templates', 'dotnet-angular', cascadePoint);

const roles = [
  { id: 'anonymous', fileId: 'anonymous', selector: '[data-testid="role-anonymous-form-builder-templates"]' },
  { id: 'tenant_user', fileId: 'tenant-user', selector: '[data-testid="role-tenant-user-form-builder-templates"]' },
  { id: 'template_designer', fileId: 'template-designer', selector: '[data-testid="role-template-designer-form-builder-templates"]' },
  { id: 'template_reviewer', fileId: 'template-reviewer', selector: '[data-testid="role-template-reviewer-form-builder-templates"]' },
  { id: 'form_operator', fileId: 'form-operator', selector: '[data-testid="role-form-operator-form-builder-templates"]' },
  { id: 'template_publisher', fileId: 'template-publisher', selector: '[data-testid="role-template-publisher-form-builder-templates"]' },
  { id: 'tenant_admin', fileId: 'tenant-admin', selector: '[data-testid="role-tenant-admin-form-builder-templates"]' },
  { id: 'platform_admin', fileId: 'platform-admin', selector: '[data-testid="role-platform-admin-form-builder-templates"]' }
];

const cells = [
  {
    id: 'C2-en-populated-desktop',
    state: 'builder',
    lang: 'en',
    viewport: { width: 1440, height: 1000 },
    pageSelector: '[data-testid="builder-page"]'
  },
  {
    id: 'C4-he-rtl-populated-desktop',
    state: 'gallery',
    lang: 'he',
    viewport: { width: 1440, height: 1000 },
    pageSelector: '[data-testid="gallery-page"]'
  },
  {
    id: 'C6-en-populated-mobile',
    state: 'submissions',
    lang: 'en',
    viewport: { width: 390, height: 980 },
    pageSelector: '[data-testid="submissions-page"]'
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
  JSON.stringify({ flow: 'FLOW-23', slug: 'form-builder-templates', cascadePoint, moduleName, expected: roles.length * cells.length, actual: evidence.length, evidence }, null, 2)
);
