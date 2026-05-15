import { chromium } from '@playwright/test';
import { mkdirSync } from 'node:fs';
import { join, resolve } from 'node:path';

const edgePath = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const evidenceRoot = resolve(process.cwd(), '..', '..', '..', '..', 'docs', 'e2e-snapshots', 'bundle-activation', 'dotnet-angular', 'local');
const baseUrl = process.env.XIIGEN_FLOW00_BASE_URL ?? 'http://127.0.0.1:4300';

mkdirSync(evidenceRoot, { recursive: true });

const shots = [
  {
    file: 'tenant-admin-desktop-en.png',
    url: `${baseUrl}/?role=tenant_admin`,
    viewport: { width: 1440, height: 1000 },
    selector: '[data-testid="tenant-admin-wizard"]',
  },
  {
    file: 'platform-admin-mobile-en.png',
    url: `${baseUrl}/?role=platform_admin`,
    viewport: { width: 390, height: 844 },
    selector: '[data-testid="platform-admin-console"]',
  },
  {
    file: 'platform-support-desktop-en.png',
    url: `${baseUrl}/?role=platform_support`,
    viewport: { width: 1440, height: 1000 },
    selector: '[data-testid="platform-support-view"]',
  },
  {
    file: 'tenant-admin-desktop-he-rtl.png',
    url: `${baseUrl}/?role=tenant_admin&lang=he`,
    viewport: { width: 1440, height: 1000 },
    selector: '[data-testid="tenant-admin-wizard"]',
    expectDirection: 'rtl',
  },
];

const browser = await chromium.launch({
  executablePath: edgePath,
  headless: true,
});

try {
  for (const shot of shots) {
    const page = await browser.newPage({ viewport: shot.viewport });
    await page.goto(shot.url, { waitUntil: 'networkidle' });
    await page.waitForSelector('[data-testid="page-bundle-activation"]', { timeout: 15000 });
    await page.waitForSelector(shot.selector, { timeout: 15000 });

    if (shot.expectDirection) {
      const dir = await page.evaluate(() => document.documentElement.dir);
      if (dir !== shot.expectDirection) {
        throw new Error(`Expected ${shot.expectDirection} direction for ${shot.file}, got ${dir}`);
      }
    }

    const outputPath = join(evidenceRoot, shot.file);
    await page.screenshot({ path: outputPath, fullPage: true });
    await page.close();
    console.log(`${shot.file} PASS`);
  }
} finally {
  await browser.close();
}
