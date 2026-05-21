import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('FLOW-14 etl-data-integration Angular surface', () => {
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

  it('renders the ETL dashboard without internal implementation language', async () => {
    const compiled = await render('?role=anonymous&state=dashboard');
    expect(compiled.querySelector('[data-testid="role-anonymous-etl-data-integration"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="etl-dashboard-page"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="query-result-card"]')).toBeTruthy();
    expect(compiled.textContent).toContain('Preview ETL readiness without tenant records');

    for (const forbidden of ['BFA', 'DNA-', 'AF station', 'DataProcessResult', 'CF-', 'FREEDOM config', 'MACHINE code']) {
      expect(compiled.textContent).not.toContain(forbidden);
    }
  });

  it('renders plan, ingestion, and schema drift evidence hooks', async () => {
    const plan = await render('?role=tenant_admin&state=plan');
    expect(plan.querySelector('[data-testid="etl-pipeline-form"]')).toBeTruthy();
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
    expect(dashboard.querySelector('[data-testid="schema-drift-panel"]')).toBeTruthy();
  });

  it('renders every canonical role variant', async () => {
    for (const role of ['anonymous', 'tenant_user', 'referral_user', 'freelancer', 'business_partner', 'event_organiser', 'tenant_admin', 'platform_admin', 'platform_support']) {
      const compiled = await render(`?role=${role}&state=dashboard`);
      const id = role.replaceAll('_', '-');
      expect(compiled.querySelector(`[data-testid="role-${id}-etl-data-integration"]`)).toBeTruthy();
    }
  });

  it('sets Hebrew language and right-to-left direction', async () => {
    const compiled = await render('?role=tenant_admin&state=ingest&lang=he');
    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(compiled.textContent).toContain('אינטגרציית נתונים');
    expect(compiled.querySelector('[data-testid="event-ingestion-page"]')).toBeTruthy();
  });

  it('supports module name adaptation through query string', async () => {
    const compiled = await render('?role=platform_support&state=dashboard&module=tessera-etl-hub');
    expect(compiled.textContent).toContain('Tessera Etl Hub');
  });
});
