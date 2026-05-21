import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('FLOW-20 ads platform UI', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App]
    }).compileComponents();
  });

  it('renders the auction page without internal protocol vocabulary', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setScreenState('auction');
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;

    expect(fixture.nativeElement.querySelector('[data-testid="auction-page"]')).toBeTruthy();
    expect(text).toContain('Budget-safe bid processing');
    expect(text).not.toContain('DataProcessResult');
    expect(text).not.toContain('CF-');
    expect(text).not.toContain('BFA');
  });

  it('shows auction controls only to advertiser and admin roles', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setScreenState('auction');
    fixture.componentInstance.setRole('business_partner');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Authorize bid');

    fixture.componentInstance.setRole('tenant_user');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('Authorize bid');
    expect(fixture.nativeElement.textContent).toContain('Preview only');
  });

  it('renders consent controls for member-facing roles', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setScreenState('consent');
    fixture.componentInstance.setRole('tenant_user');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="consent-page"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Update consent');
  });

  it('renders review queue for ad moderator roles', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setScreenState('review');
    fixture.componentInstance.setRole('ad_moderator');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="review-page"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Approve creative');
  });

  it('sets html language and direction for Hebrew evidence', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.applyLanguage('he');
    fixture.detectChanges();

    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
  });
});
