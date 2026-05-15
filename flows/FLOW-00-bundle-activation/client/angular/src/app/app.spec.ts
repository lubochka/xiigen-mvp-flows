import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('Bundle Activation App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
    }).compileComponents();
  });

  it('renders the FLOW-00 title', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Bundle Activation');
  });

  it('shows the tenant admin activation wizard by default', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="tenant-admin-wizard"]')).toBeTruthy();
  });

  it('switches platform support to read-only controls', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;

    app.setRole('platform_support');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="platform-support-view"]')).toBeTruthy();
    expect(compiled.querySelector('button[disabled]')?.textContent).toContain('Force-activate');
  });

  it('updates html direction for Hebrew evidence', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;

    app.applyLanguage('he');
    fixture.detectChanges();

    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
  });

  it('accepts role query parameters for visual evidence entrypoints', () => {
    window.history.pushState(null, '', '?role=platform_admin&lang=he');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="platform-admin-console"]')).toBeTruthy();
    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    window.history.pushState(null, '', '/');
  });
});
