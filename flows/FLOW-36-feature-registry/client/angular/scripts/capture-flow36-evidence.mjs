import { chromium } from 'playwright';
import fs from 'node:fs/promises';
import path from 'node:path';

const baseUrl = process.env.FLOW36_BASE_URL ?? 'http://127.0.0.1:4336';
const outputRoot = process.env.FLOW36_EVIDENCE_DIR ?? path.resolve('..', '..', '..', '..', 'docs', 'e2e-snapshots', 'feature-registry', 'dotnet-angular', 'C0-source-flow');
const moduleName = process.env.FLOW36_MODULE_NAME ?? 'Feature Registry';
const roles = ['platform_admin', 'platform_support', 'tenant_admin'];
const cells = [
  { id: 'C2', state: 'features', lang: 'en', width: 1440, height: 1000, label: 'en-populated-desktop' },
  { id: 'C4', state: 'decisions', lang: 'he', width: 1440, height: 1000, label: 'he-rtl-populated-desktop' },
  { id: 'C6', state: 'simulations', lang: 'en', width: 390, height: 900, label: 'en-populated-mobile' }
];

await fs.mkdir(outputRoot, { recursive: true });
const browser = await chromium.launch({ headless: true });
const evidence = [];
try {
  for (const role of roles) {
    for (const cell of cells) {
      const page = await browser.newPage({ viewport: { width: cell.width, height: cell.height } });
      const url = `${baseUrl}/?role=${encodeURIComponent(role)}&state=${cell.state}&lang=${cell.lang}&module=${encodeURIComponent(moduleName)}`;
      await page.goto(url, { waitUntil: 'networkidle' });
      await page.locator(`[data-testid="role-${role.replaceAll('_', '-')}-feature-registry"]`).waitFor({ timeout: 5000 });
      const filename = `${role.replaceAll('_', '-')}-${cell.id}-${cell.label}.png`;
      const fullPath = path.join(outputRoot, filename);
      await page.screenshot({ path: fullPath, fullPage: true });
      const stat = await fs.stat(fullPath);
      evidence.push({ role, cell: cell.id, lang: cell.lang, state: cell.state, path: fullPath, bytes: stat.size });
      await page.close();
    }
  }
} finally {
  await browser.close();
}

await fs.writeFile(path.join(outputRoot, 'visual-evidence-manifest.json'), JSON.stringify({ flowId: 'FLOW-36', flowSlug: 'feature-registry', expectedCount: roles.length * cells.length, actualCount: evidence.length, zeroByteCount: evidence.filter(item => item.bytes === 0).length, evidence }, null, 2));

