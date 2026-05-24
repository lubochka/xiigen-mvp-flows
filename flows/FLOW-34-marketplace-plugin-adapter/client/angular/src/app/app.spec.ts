import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('MarketplacePluginAdapter App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [App] }).compileComponents();
  });

  it('renders adapter marketplace copy without leaking internal rule identifiers', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Porting decisions become thin adapters');
    expect(text).toContain('adapter gates clear');
    expect(text).not.toContain('CF-');
    expect(text).not.toContain('BFA');
  });

  it('shows operator controls for platform admin', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setRole('platform_admin');
    fixture.componentInstance.setScreenState('planning');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('approve porting decisions');
    expect(fixture.componentInstance.canOperate()).toBe(true);
  });

  it('keeps platform support on inspection-only copy', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setRole('platform_support');
    fixture.componentInstance.setScreenState('audit');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('inspect adapter audit evidence');
    expect(fixture.componentInstance.canInspect()).toBe(true);
    expect(fixture.componentInstance.canOperate()).toBe(false);
  });

  it('supports Hebrew right-to-left evidence cells', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.applyLanguage('he');
    fixture.detectChanges();

    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(fixture.nativeElement.textContent).toContain('מתאם תוספי מרקטפלייס');
  });

  it('recognizes all eight role branches from the FLOW-34 role map', () => {
    const fixture = TestBed.createComponent(App);

    expect(fixture.componentInstance.roles.length).toBe(8);
    fixture.componentInstance.roles.forEach(role => {
      fixture.componentInstance.setRole(role);
      expect(fixture.componentInstance.roleTestId()).toContain('marketplace-plugin-adapter');
    });
  });
});
