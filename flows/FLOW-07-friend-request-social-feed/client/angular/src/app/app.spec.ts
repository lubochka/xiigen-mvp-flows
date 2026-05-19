import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('Friend Request Social Feed App', () => {
  beforeEach(async () => {
    window.history.pushState(null, '', '/');
    document.documentElement.lang = 'en';
    document.documentElement.dir = 'ltr';
    await TestBed.configureTestingModule({
      imports: [App],
    }).compileComponents();
  });

  it('renders friend request evidence for anonymous visitors', async () => {
    window.history.pushState(null, '', '?role=anonymous&state=request');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="page-friendrequest"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="role-anonymous-friend-feed"]')).toBeTruthy();
    expect(compiled.textContent).toContain('Safe decision path');
  });

  it('renders marketplace visitor social proof separately from anonymous', () => {
    window.history.pushState(null, '', '?role=public_marketplace_visitor&state=request');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="role-public-marketplace-visitor-friend-feed"]')).toBeTruthy();
    expect(compiled.textContent).toContain('Marketplace social proof');
  });

  it('renders connection graph and bidirectional evidence for tenant admins', () => {
    window.history.pushState(null, '', '?role=tenant_admin&state=connections');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="page-connections"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="page-socialgraph"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="role-tenant-admin-friend-feed"]')).toBeTruthy();
  });

  it('renders personalized social feed for tenant users', () => {
    window.history.pushState(null, '', '?role=tenant_user&state=feed');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="page-social-feed"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="role-tenant-user-friend-feed"]')).toBeTruthy();
    expect(compiled.textContent).toContain('A zero score is still preserved');
  });

  it('renders freelancer and business partner roles', () => {
    window.history.pushState(null, '', '?role=business_partner&state=connections&module=tenant-b-friend-request-social-feed');
    const businessFixture = TestBed.createComponent(App);
    businessFixture.detectChanges();
    expect((businessFixture.nativeElement as HTMLElement).querySelector('[data-testid="role-business-partner-friend-feed"]')).toBeTruthy();
    expect((businessFixture.nativeElement as HTMLElement).textContent).toContain('Tenant B Friend Request Social Feed');

    window.history.pushState(null, '', '?role=freelancer&state=feed');
    const freelancerFixture = TestBed.createComponent(App);
    freelancerFixture.detectChanges();
    expect((freelancerFixture.nativeElement as HTMLElement).querySelector('[data-testid="role-freelancer-friend-feed"]')).toBeTruthy();
  });

  it('renders platform admin oversight role', () => {
    window.history.pushState(null, '', '?role=platform_admin&state=connections');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="role-platform-admin-friend-feed"]')).toBeTruthy();
    expect(compiled.textContent).toContain('Platform social oversight');
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
    window.history.pushState(null, '', '?role=tenant_admin&lang=he&state=connections&module=tenant-a-friend-request-social-feed');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="role-tenant-admin-friend-feed"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="page-connections"]')).toBeTruthy();
    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
  });

  it('uses all seven FLOW-07 visual roles', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;

    expect(app.roles).toEqual([
      'anonymous',
      'public_marketplace_visitor',
      'tenant_user',
      'tenant_admin',
      'freelancer',
      'business_partner',
      'platform_admin'
    ]);
  });
});
