import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('BFA Cross-Flow Governance App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [App] }).compileComponents();
  });

  it('renders the governance workspace without leaking rule identifiers to users', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Governance arbiter');
    expect(text).toContain('Change intake starts only after the rule pack is complete');
    expect(text).not.toMatch(/DNA-[1-9]|AF station|DataProcessResult|CF-[0-9]|T[0-9]{3}/);
  });

  it('shows impact review posture for Hebrew RTL evidence', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.applyLanguage('he');
    component.setScreenState('impact');
    component.setRole('arbiter_reviewer');
    fixture.detectChanges();

    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(fixture.nativeElement.querySelector('[data-testid="impact-page"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Breaking changes route to arbitration with evidence');
  });

  it('keeps anonymous users on public preview copy', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.setRole('anonymous');
    fixture.detectChanges();

    expect(component.publicView()).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('Public visitors see only the governance posture');
  });

  it('renders release evidence for the approval gate', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.setScreenState('release');
    component.setRole('release_manager');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="release-page"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Approval requires scan, impact, and stress evidence');
    expect(fixture.nativeElement.textContent).toContain('persist-before-emit');
  });

  it('keeps release authority restricted to release roles', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.setRole('release_manager');
    component.setReadinessScore(99);
    fixture.detectChanges();

    expect(component.canApprove()).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('99');
  });
});
