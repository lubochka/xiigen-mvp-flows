import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('Profile Enrichment App', () => {
  beforeEach(async () => {
    window.history.pushState(null, '', '/');
    document.documentElement.lang = 'en';
    document.documentElement.dir = 'ltr';
    await TestBed.configureTestingModule({
      imports: [App],
    }).compileComponents();
  });

  it('renders questionnaire form for tenant users', async () => {
    window.history.pushState(null, '', '?role=tenant_user&state=questionnaire');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="page-questionnaire"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="questionnaire-form"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="industry-input"]')).toBeTruthy();
  });

  it('renders partial matching as a successful state', () => {
    window.history.pushState(null, '', '?role=business_partner&state=matching-partial');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="page-matching"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="matching-partial"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="partial-results-notice"]')?.textContent).toContain('Partial results');
  });

  it('renders personalization completion and the distinct completion event', () => {
    window.history.pushState(null, '', '?role=freelancer&state=personalization-complete');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="page-personalization"]')).toBeTruthy();
    expect(compiled.querySelectorAll('[data-testid="feed-item"]').length).toBe(4);
    expect(compiled.querySelector('[data-testid="personalization-completed-event"]')?.textContent).toContain("You're all set");
  });

  it('fails closed for anonymous viewers', () => {
    window.history.pushState(null, '', '?role=anonymous&state=questionnaire');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="role-anonymous-denied"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="questionnaire-form"]')).toBeFalsy();
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
    window.history.pushState(null, '', '?role=platform_admin&lang=he&state=matching-complete&module=tenant-a-profile-enrichment');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="role-platform-admin-profile"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="matching-complete"]')).toBeTruthy();
    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
  });
});
