import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('FLOW-08 Marketplace Angular surface', () => {
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

  it('renders public anonymous catalog without internal language', async () => {
    const compiled = await render('?role=anonymous&state=catalog');
    expect(compiled.querySelector('[data-testid="role-anonymous-marketplace"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="page-marketplace-catalog"]')).toBeTruthy();
    expect(compiled.textContent).toContain('Browse trusted offers');
    expect(compiled.textContent).not.toContain('DataProcessResult');
    expect(compiled.textContent).not.toContain('CF-');
  });

  it('renders marketplace visitor shared listing branch', async () => {
    const compiled = await render('?role=public_marketplace_visitor&state=catalog');
    expect(compiled.querySelector('[data-testid="role-public-marketplace-visitor-marketplace"]')).toBeTruthy();
    expect(compiled.textContent).toContain('Shared listing preview');
  });

  it('renders buyer and seller roles on one marketplace route', async () => {
    const buyer = await render('?role=tenant_user&state=catalog');
    expect(buyer.querySelector('[data-testid="role-tenant-user-marketplace"]')).toBeTruthy();
    expect(buyer.textContent).toContain('Contact seller');

    const seller = await render('?role=tenant_admin&state=publish');
    expect(seller.querySelector('[data-testid="role-tenant-admin-marketplace"]')).toBeTruthy();
    expect(seller.querySelector('[data-testid="page-marketplace-publish"]')).toBeTruthy();
    expect(seller.textContent).toContain('Publish listing');
  });

  it('renders referral, freelancer, partner, organiser, admin, and support variants', async () => {
    for (const role of ['referral_user', 'freelancer', 'business_partner', 'event_organiser', 'platform_admin', 'platform_support']) {
      const compiled = await render(`?role=${role}&state=analytics`);
      const id = role.replaceAll('_', '-');
      expect(compiled.querySelector(`[data-testid="role-${id}-marketplace"]`)).toBeTruthy();
      expect(compiled.querySelector('[data-testid="page-marketplace-analytics"]')).toBeTruthy();
    }
  });

  it('sets Hebrew language and right-to-left direction', async () => {
    const compiled = await render('?role=tenant_admin&state=publish&lang=he');
    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(compiled.textContent).toContain('שוק עסקי');
  });

  it('supports module name adaptation through query string', async () => {
    const compiled = await render('?role=business_partner&state=catalog&module=curated-partner-market');
    expect(compiled.textContent).toContain('Curated Partner Market');
  });
});
