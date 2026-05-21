import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('FLOW-18 visual-flow-engine Angular surface', () => {
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

  it('renders canvas, publisher, and debug surfaces without internal implementation language', async () => {
    const canvas = await render('?role=flow_author&state=canvas');
    expect(canvas.querySelector('[data-testid="role-flow-author-visual-flow-engine"]')).toBeTruthy();
    expect(canvas.querySelector('[data-testid="visual-flow-engine-page"]')).toBeTruthy();
    expect(canvas.querySelector('[data-testid="flow-canvas-page"]')).toBeTruthy();
    expect(canvas.textContent).toContain('Drag-ready nodes');

    const publisher = await render('?role=prompt_approver&state=publisher');
    expect(publisher.querySelector('[data-testid="flow-publisher-page"]')).toBeTruthy();
    expect(publisher.querySelector('[data-testid="deploy-flow-button"]')).toBeTruthy();

    const debug = await render('?role=human_decision_maker&state=debug');
    expect(debug.querySelector('[data-testid="flow-debug-page"]')).toBeTruthy();
    expect(debug.querySelector('[data-testid="test-trace-panel"]')).toBeTruthy();

    const forbiddenTerms = ['B' + 'FA', 'D' + 'NA-', 'AF ' + 'station', 'Data' + 'ProcessResult', 'C' + 'F-', 'FREE' + 'DOM config', 'MACHINE ' + 'code'];
    for (const forbidden of forbiddenTerms) {
      expect(canvas.textContent).not.toContain(forbidden);
      expect(publisher.textContent).not.toContain(forbidden);
      expect(debug.textContent).not.toContain(forbidden);
    }
  });

  it('renders every canonical role variant', async () => {
    for (const role of ['anonymous', 'public_visitor', 'tenant_user', 'tenant_admin', 'referral_user', 'flow_author', 'prompt_approver', 'human_decision_maker', 'platform_admin', 'platform_support']) {
      const compiled = await render(`?role=${role}&state=canvas`);
      const id = role.replaceAll('_', '-');
      expect(compiled.querySelector(`[data-testid="role-${id}-visual-flow-engine"]`)).toBeTruthy();
    }
  });

  it('sets Hebrew language and right-to-left direction', async () => {
    const compiled = await render('?role=tenant_admin&state=publisher&lang=he');
    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(compiled.querySelector('[data-testid="flow-publisher-page"]')).toBeTruthy();
  });

  it('supports module name adaptation through query string', async () => {
    const compiled = await render('?role=platform_support&state=debug&module=tessera-flow-orchestrator');
    expect(compiled.textContent).toContain('Tessera Flow Orchestrator');
  });
});
