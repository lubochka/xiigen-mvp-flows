import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('FLOW-17 freelancer-marketplace Angular surface', () => {
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

  it('renders gig, milestone, and review surfaces without internal implementation language', async () => {
    const gig = await render('?role=anonymous&state=gig');
    expect(gig.querySelector('[data-testid="role-anonymous-freelancer-marketplace"]')).toBeTruthy();
    expect(gig.querySelector('[data-testid="freelancer-marketplace-page"]')).toBeTruthy();
    expect(gig.querySelector('[data-testid="gig-posting-page"]')).toBeTruthy();
    expect(gig.textContent).toContain('Clients accept freelancer bids');

    const milestones = await render('?role=tenant_admin&state=milestones');
    expect(milestones.querySelector('[data-testid="milestone-dashboard-page"]')).toBeTruthy();
    expect(milestones.querySelector('[data-testid="release-milestone-button"]')).toBeTruthy();

    const review = await render('?role=freelancer&state=review');
    expect(review.querySelector('[data-testid="review-panel"]')).toBeTruthy();
    expect(review.querySelector('[data-testid="review-direction-card"]')).toBeTruthy();

    const forbiddenTerms = ['B' + 'FA', 'D' + 'NA-', 'AF ' + 'station', 'Data' + 'ProcessResult', 'C' + 'F-', 'FREE' + 'DOM config', 'MACHINE ' + 'code'];
    for (const forbidden of forbiddenTerms) {
      expect(gig.textContent).not.toContain(forbidden);
      expect(milestones.textContent).not.toContain(forbidden);
      expect(review.textContent).not.toContain(forbidden);
    }
  });

  it('renders every canonical role variant', async () => {
    for (const role of ['anonymous', 'public_visitor', 'tenant_user', 'tenant_admin', 'referral_user', 'freelancer', 'business_partner', 'event_organiser', 'platform_admin', 'platform_support']) {
      const compiled = await render(`?role=${role}&state=gig`);
      const id = role.replaceAll('_', '-');
      expect(compiled.querySelector(`[data-testid="role-${id}-freelancer-marketplace"]`)).toBeTruthy();
    }
  });

  it('sets Hebrew language and right-to-left direction', async () => {
    const compiled = await render('?role=tenant_admin&state=milestones&lang=he');
    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(compiled.querySelector('[data-testid="milestone-dashboard-page"]')).toBeTruthy();
  });

  it('supports module name adaptation through query string', async () => {
    const compiled = await render('?role=platform_support&state=review&module=tessera-freelancer-trust');
    expect(compiled.textContent).toContain('Tessera Freelancer Trust');
  });
});
