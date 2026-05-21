import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole =
  | 'anonymous'
  | 'public_visitor'
  | 'tenant_user'
  | 'tenant_admin'
  | 'referral_user'
  | 'freelancer'
  | 'business_partner'
  | 'event_organiser'
  | 'platform_admin'
  | 'platform_support';
type Language = 'en' | 'he';
type ScreenState = 'gig' | 'milestones' | 'review';

interface LedgerRow {
  label: string;
  value: string;
  tone: 'good' | 'watch' | 'neutral';
}

@Component({
  selector: 'app-root',
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  readonly roles: ViewerRole[] = [
    'anonymous',
    'public_visitor',
    'tenant_user',
    'tenant_admin',
    'referral_user',
    'freelancer',
    'business_partner',
    'event_organiser',
    'platform_admin',
    'platform_support'
  ];
  readonly role = signal<ViewerRole>('tenant_admin');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('gig');
  readonly moduleName = signal('Freelancer Marketplace');
  readonly budget = signal(1400);
  readonly milestoneTotal = signal(1400);
  readonly reviewComment = signal('Clear delivery and fast handoff');

  readonly pageTitle = computed(() => this.language() === 'he' ? 'שוק פרילנסרים' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-freelancer-marketplace`);
  readonly canOperate = computed(() => !['anonymous', 'public_visitor'].includes(this.role()));
  readonly canRelease = computed(() => ['tenant_admin', 'platform_admin', 'platform_support', 'event_organiser'].includes(this.role()));
  readonly canReview = computed(() => ['tenant_user', 'freelancer', 'business_partner', 'tenant_admin', 'platform_admin', 'platform_support'].includes(this.role()));

  readonly gigRows: LedgerRow[] = [
    { label: 'Ownership check', value: 'Before lock', tone: 'good' },
    { label: 'Acceptance lock', value: 'Exclusive', tone: 'good' },
    { label: 'Bid status', value: 'Open', tone: 'good' },
    { label: 'Audit trail', value: 'Written before event', tone: 'watch' }
  ];

  readonly milestoneRows: LedgerRow[] = [
    { label: 'Immutable fields', value: 'Client, gig, freelancer', tone: 'good' },
    { label: 'Milestone total', value: 'Balanced', tone: 'good' },
    { label: 'Delivery gate', value: 'Submitted first', tone: 'watch' },
    { label: 'Compensation', value: 'Reverse order', tone: 'neutral' }
  ];

  constructor(@Inject(DOCUMENT) private readonly document: Document) {
    const params = new URLSearchParams(this.document.defaultView?.location.search ?? '');
    const requestedRole = params.get('role');
    const requestedState = params.get('state');
    const requestedModule = params.get('module');
    const requestedLanguage: Language = params.get('lang') === 'he' ? 'he' : 'en';

    if (this.isViewerRole(requestedRole)) {
      this.role.set(requestedRole);
    }
    if (this.isScreenState(requestedState)) {
      this.screenState.set(requestedState);
    }
    if (requestedModule) {
      this.moduleName.set(this.toTitle(requestedModule));
    }
    this.applyLanguage(requestedLanguage);
  }

  setRole(value: ViewerRole): void {
    this.role.set(value);
  }

  setScreenState(value: ScreenState): void {
    this.screenState.set(value);
  }

  setBudget(value: string): void {
    const parsed = Number(value);
    this.budget.set(Number.isFinite(parsed) && parsed > 0 ? parsed : 0);
  }

  setMilestoneTotal(value: string): void {
    const parsed = Number(value);
    this.milestoneTotal.set(Number.isFinite(parsed) && parsed > 0 ? parsed : 0);
  }

  setReviewComment(value: string): void {
    this.reviewComment.set(value);
  }

  applyLanguage(value: Language): void {
    this.language.set(value);
    this.document.documentElement.lang = value;
    this.document.documentElement.dir = value === 'he' ? 'rtl' : 'ltr';
  }

  roleLabel(role: ViewerRole): string {
    const labels: Record<ViewerRole, string> = {
      anonymous: 'Guest viewer',
      public_visitor: 'Public visitor',
      tenant_user: 'Client',
      tenant_admin: 'Workspace admin',
      referral_user: 'Referral viewer',
      freelancer: 'Freelancer',
      business_partner: 'Partner manager',
      event_organiser: 'Event organiser',
      platform_admin: 'Platform admin',
      platform_support: 'Support specialist'
    };
    return labels[role];
  }

  roleHeading(): string {
    const headings: Record<ViewerRole, string> = {
      anonymous: 'Preview public gigs without private contract data',
      public_visitor: 'Compare services before signing in',
      tenant_user: 'Accept bids and approve milestone releases',
      tenant_admin: 'Govern contracts, releases, and disputes',
      referral_user: 'Track referred marketplace opportunities',
      freelancer: 'Submit delivery and review completed work',
      business_partner: 'Coordinate partner contracts and reviews',
      event_organiser: 'Coordinate service providers for event work',
      platform_admin: 'Audit escrow, disputes, and tenant boundaries',
      platform_support: 'Resolve stuck locks and contract conflicts'
    };
    return headings[this.role()];
  }

  translated(label: string): string {
    if (this.language() === 'en') {
      return label;
    }
    const labels: Record<string, string> = {
      'Freelancer Marketplace': 'שוק פרילנסרים',
      'Role': 'תפקיד',
      'Gig acceptance': 'קבלת הצעה',
      'Milestones': 'אבני דרך',
      'Review': 'ביקורת',
      'Budget': 'תקציב',
      'Milestone total': 'סכום אבני דרך',
      'Accept bid': 'קבלת הצעה',
      'Release milestone': 'שחרור אבן דרך',
      'Open dispute': 'פתיחת מחלוקת',
      'Submit review': 'שליחת ביקורת'
    };
    return labels[label] ?? label;
  }

  private isViewerRole(value: string | null): value is ViewerRole {
    return this.roles.includes(value as ViewerRole);
  }

  private isScreenState(value: string | null): value is ScreenState {
    return ['gig', 'milestones', 'review'].includes(value ?? '');
  }

  private toTitle(value: string): string {
    return value
      .split(/[-_\s]+/)
      .filter(Boolean)
      .map(part => `${part.charAt(0).toUpperCase()}${part.slice(1)}`)
      .join(' ');
  }
}
