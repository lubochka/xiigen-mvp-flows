import { chromium } from '@playwright/test';
import { mkdirSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';

const baseUrl = process.env.XIIGEN_FLOW03_BASE_URL ?? 'http://127.0.0.1:4303';
const cascadePoint = process.env.XIIGEN_CASCADE_POINT ?? 'C0-source-flow';
const moduleName = process.env.XIIGEN_FLOW_MODULE_NAME ?? 'event-management';
const evidenceRoot = resolve(
  process.cwd(),
  '..',
  '..',
  '..',
  '..',
  'docs',
  'e2e-snapshots',
  'event-management',
  'dotnet-angular',
  cascadePoint,
);

mkdirSync(evidenceRoot, { recursive: true });

const roles = [
  { id: 'anonymous', selector: '[data-testid="role-anonymous-denied"]' },
  { id: 'tenant_user', selector: '[data-testid="role-tenant-user-event"]' },
  { id: 'tenant_admin', selector: '[data-testid="role-tenant-admin-event"]' },
  { id: 'freelancer', selector: '[data-testid="role-freelancer-event"]' },
  { id: 'business_partner', selector: '[data-testid="role-business-partner-event"]' },
  { id: 'event_organiser', selector: '[data-testid="role-event-organiser-event"]' },
  { id: 'platform_admin', selector: '[data-testid="role-platform-admin-event"]' },
];

const cells = [
  {
    id: 'desktop-en-creation',
    lang: 'en',
    state: 'creation',
    pageSelector: '[data-testid="page-creation"]',
    viewport: { width: 1440, height: 1000 },
    expectDirection: 'ltr',
  },
  {
    id: 'desktop-he-rtl-promotion',
    lang: 'he',
    state: 'promotion',
    pageSelector: '[data-testid="page-promotion"]',
    viewport: { width: 1440, height: 1000 },
    expectDirection: 'rtl',
  },
  {
    id: 'mobile-en-registration',
    lang: 'en',
    state: 'registration',
    pageSelector: '[data-testid="page-registration"]',
    viewport: { width: 390, height: 844 },
    expectDirection: 'ltr',
  },
];

const shots = roles.flatMap((role) =>
  cells.map((cell) => ({
    file: `${role.id}-${cell.id}.png`,
    url: `${baseUrl}/?role=${role.id}&lang=${cell.lang}&module=${encodeURIComponent(moduleName)}&state=${cell.state}`,
    viewport: cell.viewport,
    pageSelector: role.id === 'anonymous' ? '[data-testid="page-creation"]' : cell.pageSelector,
    selector: role.selector,
    role: role.id,
    cell: cell.id,
    expectDirection: cell.expectDirection,
  })),
);

const browserExecutable = process.env.XIIGEN_BROWSER_EXECUTABLE;
const browser = await chromium.launch(browserExecutable
  ? { executablePath: browserExecutable, headless: true }
  : { headless: true });

try {
  const manifest = {
    flowId: 'FLOW-03',
    flowSlug: 'event-management',
    cascadePoint,
    moduleName,
    roleCount: roles.length,
    cellCount: cells.length,
    expectedPngCount: roles.length * cells.length,
    screenshots: [],
  };

  for (const shot of shots) {
    const page = await browser.newPage({ viewport: shot.viewport });
    await page.goto(shot.url, { waitUntil: 'networkidle' });
    await page.waitForSelector('[data-testid="page-event-management"]', { timeout: 15000 });
    await page.waitForSelector(shot.pageSelector, { timeout: 15000 });
    await page.waitForSelector(shot.selector, { timeout: 15000 });

    const htmlState = await page.evaluate(() => ({
      dir: document.documentElement.dir,
      lang: document.documentElement.lang,
    }));

    if (htmlState.dir !== shot.expectDirection) {
      throw new Error(`Expected ${shot.expectDirection} direction for ${shot.file}, got ${htmlState.dir}`);
    }

    const outputPath = join(evidenceRoot, shot.file);
    await page.screenshot({ path: outputPath, fullPage: true });
    manifest.screenshots.push({
      role: shot.role,
      cell: shot.cell,
      path: outputPath,
      lang: htmlState.lang,
      dir: htmlState.dir,
    });
    await page.close();
    console.log(`${shot.file} PASS`);
  }

  if (manifest.screenshots.length !== manifest.expectedPngCount) {
    throw new Error(`Expected ${manifest.expectedPngCount} screenshots, captured ${manifest.screenshots.length}`);
  }

  writeFileSync(join(evidenceRoot, 'visual-evidence-manifest.json'), `${JSON.stringify(manifest, null, 2)}\n`, 'utf8');
} finally {
  await browser.close();
}

