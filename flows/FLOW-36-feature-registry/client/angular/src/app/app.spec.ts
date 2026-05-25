import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('FeatureRegistry App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [App] }).compileComponents();
  });

  it('renders feature registry governance copy without leaking internal rule identifiers', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Stable FT records move through signal aggregation');
    expect(text).toContain('porting evidence complete');
    expect(text).not.toContain('CF-');
    expect(text).not.toContain('BFA');
  });

  it('shows operator controls for platform admin', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setRole('platform_admin');
    fixture.componentInstance.setScreenState('features');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('classify features');
    expect(fixture.componentInstance.canOperate()).toBe(true);
  });

  it('keeps platform support on inspection-only copy', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setRole('platform_support');
    fixture.componentInstance.setScreenState('adapters');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('inspect registry audit evidence');
    expect(fixture.componentInstance.canInspect()).toBe(true);
    expect(fixture.componentInstance.canOperate()).toBe(false);
  });

  it('keeps tenant admin on tenant capability read-only copy', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setRole('tenant_admin');
    fixture.componentInstance.setScreenState('decisions');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('active tenant capabilities');
    expect(fixture.componentInstance.canInspect()).toBe(true);
    expect(fixture.componentInstance.canOperate()).toBe(false);
  });

  it('supports Hebrew right-to-left evidence cells', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.applyLanguage('he');
    fixture.detectChanges();

    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(fixture.nativeElement.textContent).toContain('מרשם יכולות');
  });

  it('recognizes all role branches from the FLOW-36 role map', () => {
    const fixture = TestBed.createComponent(App);

    expect(fixture.componentInstance.roles.length).toBe(3);
    fixture.componentInstance.roles.forEach(role => {
      fixture.componentInstance.setRole(role);
      expect(fixture.componentInstance.roleTestId()).toContain('feature-registry');
    });
  });
});
