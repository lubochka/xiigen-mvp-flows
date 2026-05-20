import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('FLOW-09 transactional-event-participation Angular surface', () => {
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

  it('renders anonymous purchase preview without internal language', async () => {
    const compiled = await render('?role=anonymous&state=purchase');
    expect(compiled.querySelector('[data-testid="role-anonymous-transactional-event-participation"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="page-transactional-event-participation-purchase"]')).toBeTruthy();
    expect(compiled.textContent).toContain('Preview available events');
    expect(compiled.textContent).not.toContain('DataProcessResult');
    expect(compiled.textContent).not.toContain('CF-');
  });

  it('renders buyer and organiser roles on the ticketing route', async () => {
    const buyer = await render('?role=tenant_user&state=purchase');
    expect(buyer.querySelector('[data-testid="role-tenant-user-transactional-event-participation"]')).toBeTruthy();
    expect(buyer.textContent).toContain('Reserve seat');

    const organiser = await render('?role=event_organiser&state=ticket');
    expect(organiser.querySelector('[data-testid="role-event-organiser-transactional-event-participation"]')).toBeTruthy();
    expect(organiser.querySelector('[data-testid="page-transactional-event-participation-ticket"]')).toBeTruthy();
    expect(organiser.textContent).toContain('Open check-in');
  });

  it('renders all supported role variants', async () => {
    for (const role of ['referral_user', 'freelancer', 'business_partner', 'tenant_admin', 'platform_admin', 'platform_support']) {
      const compiled = await render(`?role=${role}&state=refund`);
      const id = role.replaceAll('_', '-');
      expect(compiled.querySelector(`[data-testid="role-${id}-transactional-event-participation"]`)).toBeTruthy();
      expect(compiled.querySelector('[data-testid="page-transactional-event-participation-refund"]')).toBeTruthy();
    }
  });

  it('sets Hebrew language and right-to-left direction', async () => {
    const compiled = await render('?role=tenant_admin&state=ticket&lang=he');
    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(compiled.textContent).toContain('כרטיסים');
  });

  it('supports module name adaptation through query string', async () => {
    const compiled = await render('?role=business_partner&state=purchase&module=partner-ticket-studio');
    expect(compiled.textContent).toContain('Partner Ticket Studio');
  });
});
