import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('Event Management App', () => {
  beforeEach(async () => {
    window.history.pushState(null, '', '/');
    document.documentElement.lang = 'en';
    document.documentElement.dir = 'ltr';
    await TestBed.configureTestingModule({
      imports: [App],
    }).compileComponents();
  });

  it('renders event creation form for event organisers', async () => {
    window.history.pushState(null, '', '?role=event_organiser&state=creation');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="page-creation"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="event-creation-form"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="title-input"]')).toBeTruthy();
  });

  it('renders registration and waitlist evidence', () => {
    window.history.pushState(null, '', '?role=tenant_user&state=registration');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="page-registration"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="registration-confirmed"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="registration-waitlist"]')).toBeTruthy();
  });

  it('renders promotion audience matching and notification plan', () => {
    window.history.pushState(null, '', '?role=business_partner&state=promotion');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="page-promotion"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="audience-match-table"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="notification-plan"]')?.textContent).toContain('Push');
  });

  it('renders analytics as a best-effort TTL-windowed state', () => {
    window.history.pushState(null, '', '?role=platform_admin&state=analytics');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="page-analytics"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="analytics-ttl-window"]')?.textContent).toContain('Windowed');
  });

  it('fails closed for anonymous viewers', () => {
    window.history.pushState(null, '', '?role=anonymous&state=creation');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="role-anonymous-denied"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="event-creation-form"]')).toBeFalsy();
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
    window.history.pushState(null, '', '?role=platform_admin&lang=he&state=promotion&module=tenant-a-event-management');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="role-platform-admin-event"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="promotion-published"]')).toBeTruthy();
    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
  });
});
