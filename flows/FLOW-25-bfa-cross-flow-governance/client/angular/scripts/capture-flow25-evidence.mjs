import { chromium } from 'playwright';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';

const baseUrl = process.env.XIIGEN_FLOW25_BASE_URL ?? 'http://127.0.0.1:4323';
const cascadePoint = process.env.XIIGEN_CASCADE_POINT ?? 'C0-source-flow';
const moduleName = process.env.XIIGEN_FLOW_MODULE_NAME ?? 'bfa-cross-flow-governance';
const outputRoot = path.resolve(process.cwd(), '..', '..', '..', '..', 'docs', 'e2e-snapshots', 'bfa-cross-flow-governance', 'dotnet-angular', cascadePoint);

const roles = [
  { id: 'anonymous', fileId: 'anonymous', selector: '[data-testid="role-anonymous-bfa-cross-flow-governance"]' },
  { id: 'governance_operator', fileId: 'governance-operator', selector: '[data-testid="role-governance-operator-bfa-cross-flow-governance"]' },
  { id: 'arbiter_reviewer', fileId: 'arbiter-reviewer', selector: '[data-testid="role-arbiter-reviewer-bfa-cross-flow-governance"]' },
  { id: 'flow_architect', fileId: 'flow-architect', selector: '[data-testid="role-flow-architect-bfa-cross-flow-governance"]' },
  { id: 'impact_analyst', fileId: 'impact-analyst', selector: '[data-testid="role-impact-analyst-bfa-cross-flow-governance"]' },
  { id: 'release_manager', fileId: 'release-manager', selector: '[data-testid="role-release-manager-bfa-cross-flow-governance"]' },
  { id: 'tenant_admin', fileId: 'tenant-admin', selector: '[data-testid="role-tenant-admin-bfa-cross-flow-governance"]' },
  { id: 'platform_admin', fileId: 'platform-admin', selector: '[data-testid="role-platform-admin-bfa-cross-flow-governance"]' }
];

const cells = [
  {
    id: 'C2-en-populated-desktop',
    state: 'rules',
    lang: 'en',
    viewport: { width: 1440, height: 1000 },
    pageSelector: '[data-testid="rules-page"]'
  },
  {
    id: 'C4-he-rtl-populated-desktop',
    state: 'impact',
    lang: 'he',
    viewport: { width: 1440, height: 1000 },
    pageSelector: '[data-testid="impact-page"]'
  },
  {
    id: 'C6-en-populated-mobile',
    state: 'release',
    lang: 'en',
    viewport: { width: 390, height: 980 },
    pageSelector: '[data-testid="release-page"]'
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
  JSON.stringify({ flow: 'FLOW-25', slug: 'bfa-cross-flow-governance', cascadePoint, moduleName, expected: roles.length * cells.length, actual: evidence.length, evidence }, null, 2)
);

