import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('FLOW-15 saas-multi-tenancy Angular surface', () => {
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

  it('renders workspace settings without internal implementation language', async () => {
    const compiled = await render('?role=anonymous&state=general');
    expect(compiled.querySelector('[data-testid="role-anonymous-saas-multi-tenancy"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="workspace-settings-page"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="general-settings-panel"]')).toBeTruthy();
    expect(compiled.textContent).toContain('Preview workspace settings without private records');

    for (const forbidden of ['BFA', 'DNA-', 'AF station', 'DataProcessResult', 'CF-', 'FREEDOM config', 'MACHINE code']) {
      expect(compiled.textContent).not.toContain(forbidden);
    }
  });

  it('renders provisioning, member, billing, integration, and lifecycle evidence hooks', async () => {
    const general = await render('?role=tenant_admin&state=general');
    expect(general.querySelector('[data-testid="tenant-provisioning-page"]')).toBeTruthy();
    expect(general.querySelector('[data-testid="workspace-name-input"]')).toBeTruthy();
    expect(general.querySelector('[data-testid="machine-locked-key-note"]')).toBeTruthy();
    expect(general.querySelector('[data-testid="identity-guard-note"]')).toBeTruthy();

    const members = await render('?role=tenant_admin&state=members');
    expect(members.querySelector('[data-testid="members-tab"]')).toBeTruthy();
    expect(members.querySelector('[data-testid="invite-email-input"]')).toBeTruthy();

    const billing = await render('?role=tenant_admin&state=billing');
    expect(billing.querySelector('[data-testid="billing-tab"]')).toBeTruthy();
    expect(billing.querySelector('[data-testid="quota-materialization-panel"]')).toBeTruthy();

    const advanced = await render('?role=platform_admin&state=advanced');
    expect(advanced.querySelector('[data-testid="tenant-lifecycle-page"]')).toBeTruthy();
    expect(advanced.querySelector('[data-testid="integrations-tab"]')).toBeTruthy();
    expect(advanced.querySelector('[data-testid="danger-zone"]')).toBeTruthy();
  });

  it('renders every canonical role variant', async () => {
    for (const role of ['anonymous', 'tenant_user', 'referral_user', 'freelancer', 'business_partner', 'event_organiser', 'tenant_admin', 'platform_admin', 'platform_support']) {
      const compiled = await render(`?role=${role}&state=general`);
      const id = role.replaceAll('_', '-');
      expect(compiled.querySelector(`[data-testid="role-${id}-saas-multi-tenancy"]`)).toBeTruthy();
    }
  });

  it('sets Hebrew language and right-to-left direction', async () => {
    const compiled = await render('?role=tenant_admin&state=members&lang=he');
    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(compiled.textContent).toContain('הגדרות סביבת עבודה');
    expect(compiled.querySelector('[data-testid="members-tab"]')).toBeTruthy();
  });

  it('supports module name adaptation through query string', async () => {
    const compiled = await render('?role=platform_support&state=general&module=tessera-workspace-governance');
    expect(compiled.textContent).toContain('Tessera Workspace Governance');
  });
});
