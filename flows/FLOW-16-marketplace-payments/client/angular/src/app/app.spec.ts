import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('FLOW-16 marketplace-payments Angular surface', () => {
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

  it('renders checkout, escrow, and admin payment surfaces without internal implementation language', async () => {
    const checkout = await render('?role=anonymous&state=checkout');
    expect(checkout.querySelector('[data-testid="role-anonymous-marketplace-payments"]')).toBeTruthy();
    expect(checkout.querySelector('[data-testid="marketplace-payments-page"]')).toBeTruthy();
    expect(checkout.querySelector('[data-testid="checkout-page"]')).toBeTruthy();
    expect(checkout.textContent).toContain('Guest checkout');

    const escrow = await render('?role=tenant_admin&state=escrow');
    expect(escrow.querySelector('[data-testid="escrow-dashboard"]')).toBeTruthy();
    expect(escrow.querySelector('[data-testid="release-funds-button"]')).toBeTruthy();

    const admin = await render('?role=platform_admin&state=admin');
    expect(admin.querySelector('[data-testid="refund-queue"]')).toBeTruthy();
    expect(admin.querySelector('[data-testid="platform-revenue-dashboard"]')).toBeTruthy();

    const forbiddenTerms = ['B' + 'FA', 'D' + 'NA-', 'AF ' + 'station', 'Data' + 'ProcessResult', 'C' + 'F-', 'FREE' + 'DOM config', 'MACHINE ' + 'code'];
    for (const forbidden of forbiddenTerms) {
      expect(checkout.textContent).not.toContain(forbidden);
      expect(escrow.textContent).not.toContain(forbidden);
      expect(admin.textContent).not.toContain(forbidden);
    }
  });

  it('renders every canonical role variant', async () => {
    for (const role of ['anonymous', 'public_visitor', 'tenant_user', 'tenant_admin', 'referral_user', 'freelancer', 'business_partner', 'event_organiser', 'platform_admin', 'platform_support']) {
      const compiled = await render(`?role=${role}&state=checkout`);
      const id = role.replaceAll('_', '-');
      expect(compiled.querySelector(`[data-testid="role-${id}-marketplace-payments"]`)).toBeTruthy();
    }
  });

  it('sets Hebrew language and right-to-left direction', async () => {
    const compiled = await render('?role=tenant_admin&state=escrow&lang=he');
    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(compiled.textContent).toContain('תשלומי שוק');
    expect(compiled.querySelector('[data-testid="escrow-dashboard"]')).toBeTruthy();
  });

  it('supports module name adaptation through query string', async () => {
    const compiled = await render('?role=platform_support&state=admin&module=tessera-payment-trust');
    expect(compiled.textContent).toContain('Tessera Payment Trust');
  });
});
