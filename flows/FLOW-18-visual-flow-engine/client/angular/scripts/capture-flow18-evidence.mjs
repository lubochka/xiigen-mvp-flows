import { chromium } from 'playwright';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';

const baseUrl = process.env.XIIGEN_FLOW18_BASE_URL ?? 'http://127.0.0.1:4317';
const cascadePoint = process.env.XIIGEN_CASCADE_POINT ?? 'C0-source-flow';
const moduleName = process.env.XIIGEN_FLOW_MODULE_NAME ?? 'visual-flow-engine';
const outputRoot = path.resolve(process.cwd(), '..', '..', '..', '..', 'docs', 'e2e-snapshots', 'visual-flow-engine', 'dotnet-angular', cascadePoint);

const roles = [
  { id: 'anonymous', fileId: 'anonymous', selector: '[data-testid="role-anonymous-visual-flow-engine"]' },
  { id: 'public_visitor', fileId: 'public-visitor', selector: '[data-testid="role-public-visitor-visual-flow-engine"]' },
  { id: 'tenant_user', fileId: 'tenant-user', selector: '[data-testid="role-tenant-user-visual-flow-engine"]' },
  { id: 'tenant_admin', fileId: 'tenant-admin', selector: '[data-testid="role-tenant-admin-visual-flow-engine"]' },
  { id: 'referral_user', fileId: 'referral-user', selector: '[data-testid="role-referral-user-visual-flow-engine"]' },
  { id: 'flow_author', fileId: 'flow-author', selector: '[data-testid="role-flow-author-visual-flow-engine"]' },
  { id: 'prompt_approver', fileId: 'prompt-approver', selector: '[data-testid="role-prompt-approver-visual-flow-engine"]' },
  { id: 'human_decision_maker', fileId: 'human-decision-maker', selector: '[data-testid="role-human-decision-maker-visual-flow-engine"]' },
  { id: 'platform_admin', fileId: 'platform-admin', selector: '[data-testid="role-platform-admin-visual-flow-engine"]' },
  { id: 'platform_support', fileId: 'platform-support', selector: '[data-testid="role-platform-support-visual-flow-engine"]' }
];

const cells = [
  {
    id: 'C2-en-populated-desktop',
    state: 'canvas',
    lang: 'en',
    viewport: { width: 1440, height: 1000 },
    pageSelector: '[data-testid="flow-canvas-page"]'
  },
  {
    id: 'C4-he-rtl-populated-desktop',
    state: 'publisher',
    lang: 'he',
    viewport: { width: 1440, height: 1000 },
    pageSelector: '[data-testid="flow-publisher-page"]'
  },
  {
    id: 'C6-en-populated-mobile',
    state: 'debug',
    lang: 'en',
    viewport: { width: 390, height: 980 },
    pageSelector: '[data-testid="flow-debug-page"]'
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
console.log(`FLOW-18 visual evidence captured: ${evidence.length} PNGs at ${outputRoot}`);
