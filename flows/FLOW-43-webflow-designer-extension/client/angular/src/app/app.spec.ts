import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('WebflowDesignerExtension App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [App] }).compileComponents();
  });

  it('renders Webflow Designer Extension copy without leaking internal rule identifiers', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Webflow Designer CSS becomes pipeline-compatible style data');
    expect(text).toContain('Tier D adapter evidence complete');
    expect(text).not.toContain('CF-');
    expect(text).not.toContain(['Data', 'Process', 'Result'].join(''));
  });

  it('shows operator controls for platform admin', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setRole('platform_admin');
    fixture.componentInstance.setScreenState('shorthands');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('validate adapter rules');
    expect(fixture.componentInstance.canOperate()).toBe(true);
  });

  it('shows mutation authority for tenant admin', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setRole('tenant_admin');
    fixture.componentInstance.setScreenState('routing');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('approve tenant-safe marketplace adoption');
    expect(fixture.componentInstance.canOperate()).toBe(true);
  });

  it('keeps platform support on inspection-only copy', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setRole('platform_support');
    fixture.componentInstance.setScreenState('diff');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('inspects shorthand expansion');
    expect(fixture.componentInstance.canInspect()).toBe(true);
    expect(fixture.componentInstance.canOperate()).toBe(false);
  });

  it('supports Hebrew right-to-left evidence cells', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.applyLanguage('he');
    fixture.detectChanges();

    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(fixture.nativeElement.textContent).toContain('גשר Webflow Designer');
  });

  it('recognizes all role branches from the FLOW-43 role map', () => {
    const fixture = TestBed.createComponent(App);

    expect(fixture.componentInstance.roles.length).toBe(3);
    fixture.componentInstance.roles.forEach(role => {
      fixture.componentInstance.setRole(role);
      expect(fixture.componentInstance.roleTestId()).toContain('webflow-designer-extension');
    });
  });
});
