import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('CanvaTextAdapter App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [App] }).compileComponents();
  });

  it('renders Canva Text Adapter copy without leaking internal rule identifiers', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Canva text records become shared XIIGen style inputs');
    expect(text).toContain('C5 adapter evidence complete');
    expect(text).not.toContain('CF-');
    expect(text).not.toContain(['Data', 'Process', 'Result'].join(''));
  });

  it('shows operator controls for platform admin', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setRole('platform_admin');
    fixture.componentInstance.setScreenState('read');
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
    fixture.componentInstance.setScreenState('publication');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('inspect package evidence');
    expect(fixture.componentInstance.canInspect()).toBe(true);
    expect(fixture.componentInstance.canOperate()).toBe(false);
  });

  it('supports Hebrew right-to-left evidence cells', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.applyLanguage('he');
    fixture.detectChanges();

    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(fixture.nativeElement.textContent).toContain('×’×©×¨ ×¢×“×›×•× ×™ ×œ×§×•×—');
  });

  it('recognizes all role branches from the FLOW-41 role map', () => {
    const fixture = TestBed.createComponent(App);

    expect(fixture.componentInstance.roles.length).toBe(3);
    fixture.componentInstance.roles.forEach(role => {
      fixture.componentInstance.setRole(role);
      expect(fixture.componentInstance.roleTestId()).toContain('canva-text-adapter');
    });
  });
});
