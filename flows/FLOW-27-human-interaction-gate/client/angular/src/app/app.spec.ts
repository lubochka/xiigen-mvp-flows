import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('Human Interaction Gate App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [App] }).compileComponents();
  });

  it('renders the human approval workspace without leaking internal rule identifiers to users', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Human approval console');
    expect(text).toContain('Human decisions block only the nodes that truly depend on them');
    expect(text).not.toMatch(/DNA-[1-9]|AF station|DataProcessResult|CF-[0-9]|T[0-9]{3}/);
  });

  it('shows claim posture for Hebrew RTL evidence', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.applyLanguage('he');
    component.setScreenState('claims');
    component.setRole('platform_support');
    fixture.detectChanges();

    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(fixture.nativeElement.querySelector('[data-testid="claims-page"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Claim expiry releases ownership without closing the task');
  });

  it('keeps anonymous users on public preview copy', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.setRole('anonymous');
    fixture.detectChanges();

    expect(component.publicView()).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('Public visitors see only a safe explanation');
  });

  it('renders completion evidence for the approval gate', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.setScreenState('completion');
    component.setRole('human_decision_maker');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="completion-page"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Runs cannot complete while required human tasks remain open');
    expect(fixture.nativeElement.textContent).toContain('ready can be declared');
  });

  it('keeps completion configuration restricted to tenant and platform admins', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.setRole('tenant_admin');
    component.setReadinessScore(99);
    fixture.detectChanges();

    expect(component.canConfigure()).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('99');
  });
});
