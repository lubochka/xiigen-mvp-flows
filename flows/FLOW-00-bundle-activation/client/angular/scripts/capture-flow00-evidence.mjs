import { chromium } from '@playwright/test';
import { mkdirSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';

const baseUrl = process.env.XIIGEN_FLOW00_BASE_URL ?? 'http://127.0.0.1:4300';
const cascadePoint = process.env.XIIGEN_CASCADE_POINT ?? 'C0-source-flow';
const evidenceRoot = resolve(
  process.cwd(),
  '..',
  '..',
  '..',
  '..',
  'docs',
  'e2e-snapshots',
  'bundle-activation',
  'dotnet-angular',
  cascadePoint,
);

mkdirSync(evidenceRoot, { recursive: true });

const roles = [
  { id: 'tenant_admin', selector: '[data-testid="tenant-admin-wizard"]' },
  { id: 'platform_admin', selector: '[data-testid="platform-admin-console"]' },
  { id: 'platform_support', selector: '[data-testid="platform-support-view"]' },
];

const cells = [
  { id: 'desktop-en', lang: 'en', viewport: { width: 1440, height: 1000 }, expectDirection: 'ltr' },
  { id: 'desktop-he-rtl', lang: 'he', viewport: { width: 1440, height: 1000 }, expectDirection: 'rtl' },
  { id: 'mobile-en', lang: 'en', viewport: { width: 390, height: 844 }, expectDirection: 'ltr' },
];

const shots = roles.flatMap((role) =>
  cells.map((cell) => ({
    file: `${role.id}-${cell.id}.png`,
    url: `${baseUrl}/?role=${role.id}&lang=${cell.lang}`,
    viewport: cell.viewport,
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

async function captureWithRetry(page, outputPath) {
  let lastError;
  for (let attempt = 1; attempt <= 3; attempt += 1) {
    try {
      await page.screenshot({ path: outputPath, fullPage: true });
      return;
    } catch (error) {
      lastError = error;
      await page.waitForTimeout(500 * attempt);
    }
  }

  throw lastError;
}

try {
  const manifest = {
    flowId: 'FLOW-00',
    flowSlug: 'bundle-activation',
    cascadePoint,
    roleCount: roles.length,
    cellCount: cells.length,
    expectedPngCount: roles.length * cells.length,
    screenshots: [],
  };

  for (const shot of shots) {
    const page = await browser.newPage({ viewport: shot.viewport });
    await page.goto(shot.url, { waitUntil: 'networkidle' });
    await page.waitForSelector('[data-testid="page-bundle-activation"]', { timeout: 15000 });
    await page.waitForSelector(shot.selector, { timeout: 15000 });

    const htmlState = await page.evaluate(() => ({
      dir: document.documentElement.dir,
      lang: document.documentElement.lang,
    }));

    if (htmlState.dir !== shot.expectDirection) {
      throw new Error(`Expected ${shot.expectDirection} direction for ${shot.file}, got ${htmlState.dir}`);
    }

    const outputPath = join(evidenceRoot, shot.file);
    await captureWithRetry(page, outputPath);
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
