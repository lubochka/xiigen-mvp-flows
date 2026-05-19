import { chromium } from '@playwright/test';
import { mkdirSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';

const baseUrl = process.env.XIIGEN_FLOW05_BASE_URL ?? 'http://127.0.0.1:4305';
const cascadePoint = process.env.XIIGEN_CASCADE_POINT ?? 'C0-source-flow';
const moduleName = process.env.XIIGEN_FLOW_MODULE_NAME ?? 'completion-gamification';
const evidenceRoot = resolve(
  process.cwd(),
  '..',
  '..',
  '..',
  '..',
  'docs',
  'e2e-snapshots',
  'completion-gamification',
  'dotnet-angular',
  cascadePoint,
);

mkdirSync(evidenceRoot, { recursive: true });

const roles = [
  { id: 'tenant_user', fileId: 'tenant-user', selector: '[data-testid="role-tenant-user-gamification"]' },
  { id: 'referral_user', fileId: 'referral-user', selector: '[data-testid="role-referral-user-gamification"]' },
  { id: 'freelancer', fileId: 'freelancer', selector: '[data-testid="role-freelancer-gamification"]' },
  { id: 'event_organiser', fileId: 'event-organiser', selector: '[data-testid="role-event-organiser-gamification"]' },
  { id: 'tenant_admin', fileId: 'tenant-admin', selector: '[data-testid="role-tenant-admin-gamification"]' },
];

const cells = [
  {
    id: 'C2-en-populated-desktop',
    lang: 'en',
    state: 'completion',
    pageSelector: '[data-testid="page-completion"]',
    viewport: { width: 1440, height: 1000 },
    expectDirection: 'ltr',
  },
  {
    id: 'C4-he-rtl-populated-desktop',
    lang: 'he',
    state: 'dashboard',
    pageSelector: '[data-testid="page-gamification-dashboard"]',
    viewport: { width: 1440, height: 1000 },
    expectDirection: 'rtl',
  },
  {
    id: 'C6-en-populated-mobile',
    lang: 'en',
    state: 'social',
    pageSelector: '[data-testid="page-social-learning"]',
    viewport: { width: 390, height: 844 },
    expectDirection: 'ltr',
  },
];

const shots = roles.flatMap((role) =>
  cells.map((cell) => ({
    file: `${role.fileId}-${cell.id}.png`,
    url: `${baseUrl}/?role=${role.id}&lang=${cell.lang}&module=${encodeURIComponent(moduleName)}&state=${cell.state}`,
    viewport: cell.viewport,
    pageSelector: cell.pageSelector,
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
    flowId: 'FLOW-05',
    flowSlug: 'completion-gamification',
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
    await page.waitForSelector('[data-testid="page-completion-gamification"]', { timeout: 15000 });
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
