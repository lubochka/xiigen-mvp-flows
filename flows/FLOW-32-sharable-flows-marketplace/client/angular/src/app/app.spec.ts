import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('SharableFlowsMarketplace App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [App] }).compileComponents();
  });

  it('renders public marketplace copy without leaking internal rule identifiers', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Visitors browse flow templates');
    expect(text).toContain('Tenants share logic packages');
    expect(text).not.toContain('CF-');
    expect(text).not.toContain('BFA');
  });

  it('shows publisher controls for freelancer role', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setRole('freelancer');
    fixture.componentInstance.setScreenState('publish');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('prepare a logic package');
    expect(fixture.componentInstance.canPublish()).toBe(true);
  });

  it('keeps tenant admins on install-focused copy', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.setRole('tenant_admin');
    fixture.componentInstance.setScreenState('install');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('install a linked or snapshot copy');
    expect(fixture.componentInstance.canInstall()).toBe(true);
  });

  it('supports Hebrew right-to-left evidence cells', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.applyLanguage('he');
    fixture.detectChanges();

    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(fixture.nativeElement.textContent).toContain('??? ?????? ?????');
  });

  it('recognizes all ten role branches from the FLOW-32 role map', () => {
    const fixture = TestBed.createComponent(App);

    expect(fixture.componentInstance.roles.length).toBe(10);
    fixture.componentInstance.roles.forEach(role => {
      fixture.componentInstance.setRole(role);
      expect(fixture.componentInstance.roleTestId()).toContain('sharable-flows-marketplace');
    });
  });
});