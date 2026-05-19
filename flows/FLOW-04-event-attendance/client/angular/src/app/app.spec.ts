import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('Event Attendance App', () => {
  beforeEach(async () => {
    window.history.pushState(null, '', '/');
    document.documentElement.lang = 'en';
    document.documentElement.dir = 'ltr';
    await TestBed.configureTestingModule({
      imports: [App],
    }).compileComponents();
  });

  it('renders RSVP evidence for members', async () => {
    window.history.pushState(null, '', '?role=tenant_user&state=rsvp');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="page-rsvp"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="role-tenant-user-attendance"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="rsvp-confirmed"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="rsvp-idempotent"]')).toBeTruthy();
  });

  it('renders waitlist FIFO evidence', () => {
    window.history.pushState(null, '', '?role=referral_user&state=waitlist');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="page-waitlist"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="role-referral-user-attendance"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="waitlist-fifo-list"]')?.textContent).toContain('FIFO');
  });

  it('renders check-in desk evidence for organisers', () => {
    window.history.pushState(null, '', '?role=event_organiser&state=checkin');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="page-checkin"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="role-event-organiser-attendance"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="checkin-scanner"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="checkin-waitlist-blocked"]')).toBeTruthy();
  });

  it('renders feedback window evidence after event end', () => {
    window.history.pushState(null, '', '?role=tenant_admin&state=feedback');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="page-feedback"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="feedback-window-open"]')?.textContent).toContain('Feedback window open');
  });

  it('renders attendance dashboard evidence', () => {
    window.history.pushState(null, '', '?role=platform_admin&state=dashboard');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="page-dashboard"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="metric-confirmed"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="dashboard-attendees"]')).toBeTruthy();
  });

  it('fails closed for anonymous viewers', () => {
    window.history.pushState(null, '', '?role=anonymous&state=rsvp');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="role-anonymous-denied"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="rsvp-confirmed"]')).toBeFalsy();
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
    window.history.pushState(null, '', '?role=business_partner&lang=he&state=feedback&module=tenant-a-event-attendance');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="role-business-partner-attendance"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="page-feedback"]')).toBeTruthy();
    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
  });
});
