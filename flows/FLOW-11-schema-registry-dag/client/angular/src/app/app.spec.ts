import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('FLOW-11 schema-registry-dag Angular surface', () => {
  async function render(url: string) {
    TestBed.resetTestingModule();
    window.history.pushState(null, '', url);
    await TestBed.configureTestingModule({ imports: [App] }).compileComponents();
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('renders anonymous registry preview without internal language', async () => {
    const compiled = await render('?role=anonymous&state=submission');
    expect(compiled.querySelector('[data-testid="role-anonymous-schema-registry-dag"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="page-schema-registry-dag-submission"]')).toBeTruthy();
    expect(compiled.textContent).toContain('Preview registry status');
    expect(compiled.textContent).not.toContain(['Data', 'Process', 'Result'].join(''));
    expect(compiled.textContent).not.toContain('CF-');
  });

  it('renders tenant admin and platform admin workflow states', async () => {
    const tenantAdmin = await render('?role=tenant_admin&state=submission');
    expect(tenantAdmin.querySelector('[data-testid="role-tenant-admin-schema-registry-dag"]')).toBeTruthy();
    expect(tenantAdmin.textContent).toContain('Submit schema');

    const platformAdmin = await render('?role=platform_admin&state=approval');
    expect(platformAdmin.querySelector('[data-testid="role-platform-admin-schema-registry-dag"]')).toBeTruthy();
    expect(platformAdmin.querySelector('[data-testid="page-schema-registry-dag-approval"]')).toBeTruthy();
    expect(platformAdmin.textContent).toContain('Open approval');
  });

  it('renders all supported role variants', async () => {
    for (const role of ['public_marketplace_visitor', 'tenant_admin', 'platform_admin', 'platform_support']) {
      const compiled = await render(`?role=${role}&state=dag`);
      const id = role.replaceAll('_', '-');
      expect(compiled.querySelector(`[data-testid="role-${id}-schema-registry-dag"]`)).toBeTruthy();
      expect(compiled.querySelector('[data-testid="page-schema-registry-dag-dag"]')).toBeTruthy();
    }
  });

  it('sets Hebrew language and right-to-left direction', async () => {
    const compiled = await render('?role=tenant_admin&state=approval&lang=he');
    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(compiled.textContent).toContain('מאגר');
  });

  it('supports module name adaptation through query string', async () => {
    const compiled = await render('?role=platform_support&state=dag&module=tenant-contract-map');
    expect(compiled.textContent).toContain('Tenant Contract Map');
  });
});
