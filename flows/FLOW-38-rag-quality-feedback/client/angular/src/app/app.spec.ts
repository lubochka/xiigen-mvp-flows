import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('RagQualityFeedback App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [App] }).compileComponents();
  });

  it('renders RAG quality feedback copy without leaking internal rule identifiers', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Cycle outcomes, pattern quality updates');
    expect(text).toContain('feedback evidence complete');
    expect(text).not.toContain('CF-');
    expect(text).not.toContain('DataProcessResult');
  });

  it('shows operator controls for platform admin', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setRole('platform_admin');
    fixture.componentInstance.setScreenState('outcomes');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('record cycle outcomes');
    expect(fixture.componentInstance.canOperate()).toBe(true);
  });

  it('shows mutation authority for tenant admin', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setRole('tenant_admin');
    fixture.componentInstance.setScreenState('quality');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('review learning outcomes');
    expect(fixture.componentInstance.canOperate()).toBe(true);
  });

  it('keeps platform support on inspection-only copy', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setRole('platform_support');
    fixture.componentInstance.setScreenState('signals');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('inspect feedback evidence');
    expect(fixture.componentInstance.canInspect()).toBe(true);
    expect(fixture.componentInstance.canOperate()).toBe(false);
  });

  it('supports Hebrew right-to-left evidence cells', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.applyLanguage('he');
    fixture.detectChanges();

    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(fixture.nativeElement.textContent).toContain('משוב איכות חיפוש חכם');
  });

  it('recognizes all role branches from the FLOW-38 role map', () => {
    const fixture = TestBed.createComponent(App);

    expect(fixture.componentInstance.roles.length).toBe(3);
    fixture.componentInstance.roles.forEach(role => {
      fixture.componentInstance.setRole(role);
      expect(fixture.componentInstance.roleTestId()).toContain('rag-quality-feedback');
    });
  });
});
