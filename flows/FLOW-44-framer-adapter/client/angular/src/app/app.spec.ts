import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('FramerAdapter App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [App] }).compileComponents();
  });

  it('renders Framer Text Elements Adapter copy without leaking internal rule identifiers', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Framer text nodes become shared XIIGen text elements');
    expect(text).toContain('Tier D adapter evidence complete');
    expect(text).not.toContain('CF-');
    expect(text).not.toContain(['Data', 'Process', 'Result'].join(''));
  });

  it('shows operator controls for platform admin', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setRole('platform_admin');
    fixture.componentInstance.setScreenState('filter');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('validate adapter rules');
    expect(fixture.componentInstance.canOperate()).toBe(true);
  });

  it('shows mutation authority for tenant admin', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setRole('tenant_admin');
    fixture.componentInstance.setScreenState('style');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('approve tenant-safe marketplace adoption');
    expect(fixture.componentInstance.canOperate()).toBe(true);
  });

  it('keeps platform support on inspection-only copy', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setRole('platform_support');
    fixture.componentInstance.setScreenState('storage');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('inspects filter results');
    expect(fixture.componentInstance.canInspect()).toBe(true);
    expect(fixture.componentInstance.canOperate()).toBe(false);
  });

  it('supports Hebrew right-to-left evidence cells', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.applyLanguage('he');
    fixture.detectChanges();

    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(fixture.nativeElement.textContent).toContain('מתאם טקסט Framer');
  });

  it('recognizes all role branches from the FLOW-44 role map', () => {
    const fixture = TestBed.createComponent(App);

    expect(fixture.componentInstance.roles.length).toBe(3);
    fixture.componentInstance.roles.forEach(role => {
      fixture.componentInstance.setRole(role);
      expect(fixture.componentInstance.roleTestId()).toContain('framer-adapter');
    });
  });
});
