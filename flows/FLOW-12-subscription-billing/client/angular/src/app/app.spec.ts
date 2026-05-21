import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('FLOW-12 subscription-billing Angular surface', () => {
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

  it('renders anonymous subscription preview without internal language', async () => {
    const compiled = await render('?role=anonymous&state=dashboard');
    expect(compiled.querySelector('[data-testid="role-anonymous-subscription-billing"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="billing-dashboard-page"]')).toBeTruthy();
    expect(compiled.textContent).toContain('Preview plans without account data');
    expect(compiled.textContent).not.toContain(['Data', 'Process', 'Result'].join(''));
    expect(compiled.textContent).not.toContain('CF-');
  });

  it('renders plan publishing and subscription form evidence hooks', async () => {
    const plan = await render('?role=tenant_admin&state=plan');
    expect(plan.querySelector('[data-testid="plan-publish-form"]')).toBeTruthy();
    expect(plan.querySelector('[data-testid="plan-id-input"]')).toBeTruthy();
    expect(plan.querySelector('[data-testid="billing-interval-select"]')).toBeTruthy();
    expect(plan.querySelector('[data-testid="occ-conflict-error"]')).toBeTruthy();

    const subscribe = await render('?role=tenant_user&state=subscribe');
    expect(subscribe.querySelector('[data-testid="subscribe-form"]')).toBeTruthy();
    expect(subscribe.querySelector('[data-testid="payment-method-input"]')).toBeTruthy();
    expect(subscribe.querySelector('[data-testid="subscribe-button"]')).toBeTruthy();
  });

  it('renders every canonical role variant', async () => {
    for (const role of ['anonymous', 'tenant_user', 'referral_user', 'freelancer', 'business_partner', 'event_organiser', 'tenant_admin', 'platform_admin', 'platform_support']) {
      const compiled = await render(`?role=${role}&state=dashboard`);
      const id = role.replaceAll('_', '-');
      expect(compiled.querySelector(`[data-testid="role-${id}-subscription-billing"]`)).toBeTruthy();
    }
  });

  it('sets Hebrew language and right-to-left direction', async () => {
    const compiled = await render('?role=tenant_admin&state=subscribe&lang=he');
    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(compiled.textContent).toContain('ניהול חיובים');
  });

  it('supports module name adaptation through query string', async () => {
    const compiled = await render('?role=platform_support&state=dashboard&module=tessera-ledger-billing');
    expect(compiled.textContent).toContain('Tessera Ledger Billing');
  });
});
