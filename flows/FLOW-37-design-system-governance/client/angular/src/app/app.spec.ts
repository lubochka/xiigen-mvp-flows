import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('DesignSystemGovernance App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [App] }).compileComponents();
  });

  it('renders design system governance copy without leaking internal rule identifiers', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Stack compatibility, prompt exclusions, design debt');
    expect(text).toContain('design governance evidence complete');
    expect(text).not.toContain('CF-');
    expect(text).not.toContain('internal build rule');
  });

  it('shows operator controls for platform admin', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setRole('platform_admin');
    fixture.componentInstance.setScreenState('classification');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('classify design systems');
    expect(fixture.componentInstance.canOperate()).toBe(true);
  });

  it('shows mutation authority for tenant admin', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setRole('tenant_admin');
    fixture.componentInstance.setScreenState('compatibility');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('register and review the design systems owned by their tenant');
    expect(fixture.componentInstance.canOperate()).toBe(true);
  });

  it('keeps platform support on inspection-only copy', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setRole('platform_support');
    fixture.componentInstance.setScreenState('debt');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('inspect governance evidence');
    expect(fixture.componentInstance.canInspect()).toBe(true);
    expect(fixture.componentInstance.canOperate()).toBe(false);
  });

  it('supports Hebrew right-to-left evidence cells', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.applyLanguage('he');
    fixture.detectChanges();

    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(fixture.nativeElement.textContent).toContain('ממשל מערכת עיצוב');
  });

  it('recognizes all role branches from the FLOW-37 role map', () => {
    const fixture = TestBed.createComponent(App);

    expect(fixture.componentInstance.roles.length).toBe(3);
    fixture.componentInstance.roles.forEach(role => {
      fixture.componentInstance.setRole(role);
      expect(fixture.componentInstance.roleTestId()).toContain('design-system-governance');
    });
  });
});
