import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('FLOW-13 data-warehouse-analytics Angular surface', () => {
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

  it('renders the analytics dashboard without internal implementation language', async () => {
    const compiled = await render('?role=anonymous&state=dashboard');
    expect(compiled.querySelector('[data-testid="role-anonymous-data-warehouse-analytics"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="warehouse-dashboard-page"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="query-result-card"]')).toBeTruthy();
    expect(compiled.textContent).toContain('Preview warehouse health without tenant records');

    for (const forbidden of ['BFA', 'DNA-', 'AF station', 'DataProcessResult', 'CF-', 'FREEDOM config', 'MACHINE code']) {
      expect(compiled.textContent).not.toContain(forbidden);
    }
  });

  it('renders plan, ingestion, and quality evidence hooks', async () => {
    const plan = await render('?role=tenant_admin&state=plan');
    expect(plan.querySelector('[data-testid="warehouse-plan-form"]')).toBeTruthy();
    expect(plan.querySelector('[data-testid="plan-id-input"]')).toBeTruthy();
    expect(plan.querySelector('[data-testid="warehouse-zone-input"]')).toBeTruthy();
    expect(plan.querySelector('[data-testid="event-schema-list"]')).toBeTruthy();
    expect(plan.querySelector('[data-testid="occ-conflict-error"]')).toBeTruthy();

    const ingest = await render('?role=tenant_admin&state=ingest');
    expect(ingest.querySelector('[data-testid="event-ingestion-form"]')).toBeTruthy();
    expect(ingest.querySelector('[data-testid="source-flow-input"]')).toBeTruthy();
    expect(ingest.querySelector('[data-testid="event-type-input"]')).toBeTruthy();
    expect(ingest.querySelector('[data-testid="metric-value-input"]')).toBeTruthy();
    expect(ingest.querySelector('[data-testid="ingest-button"]')).toBeTruthy();

    const dashboard = await render('?role=platform_admin&state=dashboard');
    expect(dashboard.querySelector('[data-testid="task-type-coverage"]')).toBeTruthy();
    expect(dashboard.querySelector('[data-testid="event-schema-count"]')).toBeTruthy();
    expect(dashboard.querySelector('[data-testid="quality-check-panel"]')).toBeTruthy();
  });

  it('renders every canonical role variant', async () => {
    for (const role of ['anonymous', 'tenant_user', 'referral_user', 'freelancer', 'business_partner', 'event_organiser', 'tenant_admin', 'platform_admin', 'platform_support']) {
      const compiled = await render(`?role=${role}&state=dashboard`);
      const id = role.replaceAll('_', '-');
      expect(compiled.querySelector(`[data-testid="role-${id}-data-warehouse-analytics"]`)).toBeTruthy();
    }
  });

  it('sets Hebrew language and right-to-left direction', async () => {
    const compiled = await render('?role=tenant_admin&state=ingest&lang=he');
    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(compiled.textContent).toContain('ניתוח מחסן נתונים');
    expect(compiled.querySelector('[data-testid="event-ingestion-page"]')).toBeTruthy();
  });

  it('supports module name adaptation through query string', async () => {
    const compiled = await render('?role=platform_support&state=dashboard&module=tessera-insight-warehouse');
    expect(compiled.textContent).toContain('Tessera Insight Warehouse');
  });
});
