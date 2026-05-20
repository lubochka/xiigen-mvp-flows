import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('FLOW-10 reviews-reputation Angular surface', () => {
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

  it('renders anonymous review preview without internal language', async () => {
    const compiled = await render('?role=anonymous&state=submission');
    expect(compiled.querySelector('[data-testid="role-anonymous-reviews-reputation"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="page-reviews-reputation-submission"]')).toBeTruthy();
    expect(compiled.textContent).toContain('Preview reputation signals');
    expect(compiled.textContent).not.toContain(['Data', 'Process', 'Result'].join(''));
    expect(compiled.textContent).not.toContain('CF-');
  });

  it('renders reviewer and owner roles on the expected routes', async () => {
    const reviewer = await render('?role=tenant_user&state=submission');
    expect(reviewer.querySelector('[data-testid="role-tenant-user-reviews-reputation"]')).toBeTruthy();
    expect(reviewer.textContent).toContain('Submit review');

    const owner = await render('?role=business_partner&state=response');
    expect(owner.querySelector('[data-testid="role-business-partner-reviews-reputation"]')).toBeTruthy();
    expect(owner.querySelector('[data-testid="page-reviews-reputation-response"]')).toBeTruthy();
    expect(owner.textContent).toContain('Respond to review');
  });

  it('renders all supported role variants', async () => {
    for (const role of ['public_marketplace_visitor', 'freelancer', 'tenant_admin', 'platform_admin', 'platform_support']) {
      const compiled = await render(`?role=${role}&state=moderation`);
      const id = role.replaceAll('_', '-');
      expect(compiled.querySelector(`[data-testid="role-${id}-reviews-reputation"]`)).toBeTruthy();
      expect(compiled.querySelector('[data-testid="page-reviews-reputation-moderation"]')).toBeTruthy();
    }
  });

  it('sets Hebrew language and right-to-left direction', async () => {
    const compiled = await render('?role=tenant_admin&state=moderation&lang=he');
    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(compiled.textContent).toContain('ביקורות');
  });

  it('supports module name adaptation through query string', async () => {
    const compiled = await render('?role=business_partner&state=response&module=partner-trust-hub');
    expect(compiled.textContent).toContain('Partner Trust Hub');
  });
});
