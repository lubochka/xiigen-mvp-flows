import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('Completion Gamification App', () => {
  beforeEach(async () => {
    window.history.pushState(null, '', '/');
    document.documentElement.lang = 'en';
    document.documentElement.dir = 'ltr';
    await TestBed.configureTestingModule({
      imports: [App],
    }).compileComponents();
  });

  it('renders completion reward evidence for learners', async () => {
    window.history.pushState(null, '', '?role=tenant_user&state=completion');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="page-completion"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="role-tenant-user-gamification"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="completion-form-submitted"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="gamification-feedback"]')?.textContent).toContain('+210');
  });

  it('renders dashboard points, level, streak, and achievements', () => {
    window.history.pushState(null, '', '?role=tenant_admin&state=dashboard');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="page-gamification-dashboard"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="total-points"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="current-level"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="streak-count"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="achievement-gallery"]')).toBeTruthy();
  });

  it('renders social learning privacy and peer grading surface', () => {
    window.history.pushState(null, '', '?role=referral_user&state=social');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="page-social-learning"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="role-referral-user-gamification"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="achievement-unlocked"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="answer-grading-form"]')).toBeTruthy();
  });

  it('renders freelancer portfolio sharing state', () => {
    window.history.pushState(null, '', '?role=freelancer&state=social&module=freelancer-completion-gamification');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="role-freelancer-gamification"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="post-builder"]')).toBeTruthy();
    expect(compiled.textContent).toContain('Freelancer Completion Gamification');
  });

  it('renders organiser progress view', () => {
    window.history.pushState(null, '', '?role=event_organiser&state=progress');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="page-learning-progress"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="role-event-organiser-gamification"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="performance-history"]')).toBeTruthy();
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
    window.history.pushState(null, '', '?role=tenant_admin&lang=he&state=dashboard&module=tenant-a-completion-gamification');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="role-tenant-admin-gamification"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="page-gamification-dashboard"]')).toBeTruthy();
    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
  });

  it('uses all five FLOW-05 visual roles', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;

    expect(app.roles).toEqual(['tenant_user', 'referral_user', 'freelancer', 'event_organiser', 'tenant_admin']);
  });
});
