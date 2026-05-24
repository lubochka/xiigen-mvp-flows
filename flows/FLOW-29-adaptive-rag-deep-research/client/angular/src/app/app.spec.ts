import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('Adaptive RAG Deep Research App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [App] }).compileComponents();
  });

  it('renders the research workspace without leaking internal rule identifiers to users', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Research workspace');
    expect(text).toContain('Research requests keep tenant scope');
    expect(text).not.toMatch(/DNA-[1-9]|AF station|DataProcessResult|CF-[0-9]|T[0-9]{3}/);
  });

  it('shows graph posture for Hebrew RTL evidence', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.applyLanguage('he');
    component.setScreenState('graph');
    component.setRole('prompt_approver');
    fixture.detectChanges();

    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(fixture.nativeElement.querySelector('[data-testid="graph-page"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Graph changes are approved');
  });

  it('keeps public visitors on capability summary copy', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.setRole('public_marketplace_visitor');
    fixture.detectChanges();

    expect(component.publicView()).toBe(true);
    expect(component.canResearch()).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('capability summary');
  });

  it('renders promotion evidence for tenant and platform admins', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.setScreenState('promotion');
    component.setRole('platform_admin');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="promotion-page"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Prompt and asset promotion');
    expect(fixture.nativeElement.textContent).toContain('quality gate passed');
  });

  it('covers all nine canonical visual evidence roles', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    expect(component.roles).toEqual([
      'anonymous',
      'public_marketplace_visitor',
      'tenant_user',
      'tenant_admin',
      'prompt_approver',
      'human_decision_maker',
      'agency_access',
      'platform_admin',
      'platform_support'
    ]);
  });
});
