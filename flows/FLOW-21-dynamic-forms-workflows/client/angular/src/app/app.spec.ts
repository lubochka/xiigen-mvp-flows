import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('FLOW-21 dynamic forms workflows UI', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App]
    }).compileComponents();
  });

  it('renders the schema page without internal protocol vocabulary', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setScreenState('schema');
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;

    expect(fixture.nativeElement.querySelector('[data-testid="schema-page"]')).toBeTruthy();
    expect(text).toContain('Versioned fields with role visibility');
    expect(text).not.toContain('DataProcessResult');
    expect(text).not.toContain('CF-');
    expect(text).not.toContain('BFA');
  });

  it('shows schema publish controls only to admin roles', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setScreenState('schema');
    fixture.componentInstance.setRole('tenant_admin');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Publish schema');

    fixture.componentInstance.setRole('tenant_user');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('Publish schema');
    expect(fixture.nativeElement.textContent).toContain('Preview only');
  });

  it('renders workflow dispatch controls for admin roles', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setScreenState('workflow');
    fixture.componentInstance.setRole('platform_admin');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="workflow-page"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Dispatch workflow');
  });

  it('renders analytics submitter view for signed-in users', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setScreenState('analytics');
    fixture.componentInstance.setRole('freelancer');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="analytics-page"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Submit form');
  });

  it('sets html language and direction for Hebrew evidence', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.applyLanguage('he');
    fixture.detectChanges();

    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
  });
});
