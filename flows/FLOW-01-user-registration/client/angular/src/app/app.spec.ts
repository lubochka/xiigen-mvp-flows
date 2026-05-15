import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('User Registration App', () => {
  beforeEach(async () => {
    window.history.pushState(null, '', '/');
    document.documentElement.lang = 'en';
    document.documentElement.dir = 'ltr';
    await TestBed.configureTestingModule({
      imports: [App],
    }).compileComponents();
  });

  it('renders public registration for anonymous visitors', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Registration');
    expect(compiled.querySelector('[data-testid="role-anonymous-public"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="submit-registration"]')).toBeTruthy();
  });

  it('switches tenant users into onboarding tasks', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;

    app.setRole('tenant_user');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="role-tenant-user-onboarding"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="complete-onboarding"]')).toBeTruthy();
  });

  it('keeps support read-only', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;

    app.setRole('platform_support');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="role-platform-support-readonly"]')).toBeTruthy();
    expect(compiled.querySelector('button[disabled]')?.textContent).toContain('Changes disabled');
  });

  it('updates html language and direction for Hebrew evidence', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;

    app.applyLanguage('he');
    fixture.detectChanges();

    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
  });

  it('accepts visual evidence query parameters', () => {
    window.history.pushState(null, '', '?role=platform_admin&lang=he&state=resend&module=tenant-a-registration');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="role-platform-admin-console"]')).toBeTruthy();
    expect(compiled.querySelector('h1')?.textContent).toContain('רישום משתמשים');
    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
  });
});
