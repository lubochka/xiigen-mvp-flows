import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('User Groups Communities App', () => {
  beforeEach(async () => {
    window.history.pushState(null, '', '/');
    document.documentElement.lang = 'en';
    document.documentElement.dir = 'ltr';
    await TestBed.configureTestingModule({
      imports: [App],
    }).compileComponents();
  });

  it('renders group discovery for anonymous visitors', async () => {
    window.history.pushState(null, '', '?role=anonymous&state=discovery');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="page-group-discovery"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="role-anonymous-groups"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="group-directory-list"]')).toBeTruthy();
  });

  it('renders marketplace visitor preview separately from anonymous', () => {
    window.history.pushState(null, '', '?role=public_marketplace_visitor&state=discovery');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="role-public-marketplace-visitor-groups"]')).toBeTruthy();
    expect(compiled.textContent).toContain('Marketplace preview');
  });

  it('renders membership status, approvals, and tier management', () => {
    window.history.pushState(null, '', '?role=tenant_admin&state=membership');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="page-membership-status"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="page-group-approval"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="page-tier-management"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="role-tenant-admin-groups"]')).toBeTruthy();
  });

  it('renders tenant member feed evidence', () => {
    window.history.pushState(null, '', '?role=tenant_user&state=feed');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="page-group-feed"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="role-tenant-user-groups"]')).toBeTruthy();
    expect(compiled.textContent).toContain('Content after approval');
  });

  it('renders freelancer feed with module adaptation name', () => {
    window.history.pushState(null, '', '?role=freelancer&state=feed&module=tenant-b-user-groups-communities');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="role-freelancer-groups"]')).toBeTruthy();
    expect(compiled.textContent).toContain('Tenant B User Groups Communities');
  });

  it('renders platform admin operations role', () => {
    window.history.pushState(null, '', '?role=platform_admin&state=membership');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="role-platform-admin-groups"]')).toBeTruthy();
    expect(compiled.textContent).toContain('Platform community operations');
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
    window.history.pushState(null, '', '?role=tenant_admin&lang=he&state=membership&module=tenant-a-user-groups-communities');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="role-tenant-admin-groups"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="page-membership-status"]')).toBeTruthy();
    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
  });

  it('uses all six FLOW-06 visual roles', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;

    expect(app.roles).toEqual([
      'anonymous',
      'public_marketplace_visitor',
      'tenant_user',
      'tenant_admin',
      'freelancer',
      'platform_admin'
    ]);
  });
});
