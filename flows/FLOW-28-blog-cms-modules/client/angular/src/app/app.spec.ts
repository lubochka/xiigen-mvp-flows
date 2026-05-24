import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('Blog CMS Modules App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [App] }).compileComponents();
  });

  it('renders the publishing workspace without leaking internal rule identifiers to users', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Publishing workspace');
    expect(text).toContain('Readers get safe published content without private tenant controls');
    expect(text).not.toMatch(/DNA-[1-9]|AF station|DataProcessResult|CF-[0-9]|T[0-9]{3}/);
  });

  it('shows authoring posture for Hebrew RTL evidence', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.applyLanguage('he');
    component.setScreenState('author');
    component.setRole('freelancer');
    fixture.detectChanges();

    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(fixture.nativeElement.querySelector('[data-testid="author-page"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Drafts autosave by revision');
  });

  it('keeps public visitors on reader-only copy', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.setRole('public_marketplace_visitor');
    fixture.detectChanges();

    expect(component.publicView()).toBe(true);
    expect(component.canAuthor()).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('safe comment entry');
  });

  it('renders operations evidence for tenant and platform admins', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.setScreenState('operations');
    component.setRole('platform_admin');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="operations-page"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Publishing state is written before events');
    expect(fixture.nativeElement.textContent).toContain('Published safely');
  });

  it('covers all nine canonical visual evidence roles', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    expect(component.roles).toEqual([
      'anonymous',
      'public_marketplace_visitor',
      'tenant_user',
      'tenant_admin',
      'freelancer',
      'business_partner',
      'event_organiser',
      'platform_admin',
      'platform_support'
    ]);
  });
});
