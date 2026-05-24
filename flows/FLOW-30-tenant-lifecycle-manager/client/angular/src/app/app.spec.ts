import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('Tenant Lifecycle Manager App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [App] }).compileComponents();
  });

  it('renders the lifecycle workspace without leaking internal rule identifiers to users', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Platform admins provision tenants');
    expect(text).toContain('Tenant setup records plan');
    expect(text).not.toMatch(/DNA-[1-9]|AF station|DataProcessResult|CF-[0-9]|T[0-9]{3}/);
  });

  it('shows health posture for Hebrew RTL evidence', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.applyLanguage('he');
    component.setScreenState('health');
    component.setRole('platform_support');
    fixture.detectChanges();

    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(fixture.nativeElement.querySelector('[data-testid="health-page"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Health probes and support timelines');
  });

  it('keeps tenant admins on own-tenant status copy', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.setRole('tenant_admin');
    fixture.detectChanges();

    expect(component.publicView()).toBe(false);
    expect(component.canResearch()).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('own tenant lifecycle status');
  });

  it('renders offboarding evidence for platform admins', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.setScreenState('offboarding');
    component.setRole('platform_admin');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="offboarding-page"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Tenant offboarding preserves audit');
    expect(fixture.nativeElement.textContent).toContain('evidence preserved');
  });

  it('covers the three FLOW-30 canonical visual evidence roles', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    expect(component.roles).toEqual(['tenant_admin', 'platform_admin', 'platform_support']);
  });
});
