import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('Design Intelligence Engine App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [App] }).compileComponents();
  });

  it('renders the design workspace without leaking internal rule identifiers to users', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Platform admins analyze design files');
    expect(text).toContain('Figma screens become tenant-scoped design intelligence records');
    expect(text).not.toMatch(/DNA-[1-9]|AF station|DataProcessResult|CF-[0-9]|T[0-9]{3}/);
  });

  it('shows token posture for Hebrew RTL evidence', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.applyLanguage('he');
    component.setScreenState('tokens');
    component.setRole('platform_support');
    fixture.detectChanges();

    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(fixture.nativeElement.querySelector('[data-testid="tokens-page"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Token and component evidence');
  });

  it('keeps tenant admins on tenant design console copy', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.setRole('tenant_admin');
    fixture.detectChanges();

    expect(component.publicView()).toBe(false);
    expect(component.canAnalyze()).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('own tenant design workspace');
  });

  it('renders governance evidence for platform admins', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.setScreenState('governance');
    component.setRole('platform_admin');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="governance-page"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Governance reports preserve compliance evidence');
    expect(fixture.nativeElement.textContent).toContain('token namespace retained');
  });

  it('covers the three FLOW-31 canonical visual evidence roles', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    expect(component.roles).toEqual(['platform_admin', 'platform_support', 'tenant_admin']);
  });
});
