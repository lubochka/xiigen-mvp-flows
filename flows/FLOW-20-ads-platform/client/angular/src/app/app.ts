import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole =
  | 'anonymous'
  | 'public_marketplace_visitor'
  | 'tenant_user'
  | 'referral_user'
  | 'freelancer'
  | 'business_partner'
  | 'tenant_admin'
  | 'ad_moderator'
  | 'platform_admin'
  | 'platform_support';
type Language = 'en' | 'he';
type ScreenState = 'auction' | 'consent' | 'review';

interface AuctionRow {
  slot: string;
  advertiser: string;
  bid: string;
  budget: string;
  state: 'eligible' | 'held' | 'review';
}

interface ConsentRow {
  audience: string;
  coverage: string;
  expiry: string;
  state: 'active' | 'attention' | 'blocked';
}

interface ReviewRow {
  creative: string;
  signal: string;
  score: string;
  state: 'approved' | 'needs-review' | 'queued';
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
    'public_marketplace_visitor',
    'tenant_user',
    'referral_user',
    'freelancer',
    'business_partner',
    'tenant_admin',
    'ad_moderator',
    'platform_admin',
    'platform_support'
  ];
  readonly role = signal<ViewerRole>('business_partner');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('auction');
  readonly moduleName = signal('Ads Platform');
  readonly selectedBudget = signal(8200);

  readonly auctionRows: AuctionRow[] = [
    { slot: 'Morning news rail', advertiser: 'Acme Travel', bid: '$20.00', budget: '$8,200', state: 'eligible' },
    { slot: 'Local events feed', advertiser: 'Northwind Arts', bid: '$16.50', budget: '$3,470', state: 'held' },
    { slot: 'Creator profile unit', advertiser: 'Tessera Studio', bid: '$11.40', budget: '$5,920', state: 'review' }
  ];

  readonly consentRows: ConsentRow[] = [
    { audience: 'Returning members', coverage: '94%', expiry: '91 days', state: 'active' },
    { audience: 'Referral guests', coverage: '82%', expiry: '44 days', state: 'active' },
    { audience: 'New visitors', coverage: '68%', expiry: '12 days', state: 'attention' }
  ];

  readonly reviewRows: ReviewRow[] = [
    { creative: 'City council awareness', signal: 'dual model match', score: '0.66', state: 'needs-review' },
    { creative: 'Weekend hotel offer', signal: 'commercial', score: '0.08', state: 'approved' },
    { creative: 'Civic donation reminder', signal: 'divergent signals', score: '0.58', state: 'queued' }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? 'ניהול פרסום בטוח' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-ads-platform`);
  readonly canOperateAuction = computed(() => ['business_partner', 'tenant_admin', 'platform_admin', 'platform_support'].includes(this.role()));
  readonly canManageConsent = computed(() => ['tenant_user', 'referral_user', 'freelancer', 'business_partner', 'tenant_admin', 'platform_admin', 'platform_support'].includes(this.role()));
  readonly canReview = computed(() => ['tenant_admin', 'ad_moderator', 'platform_admin', 'platform_support'].includes(this.role()));
  readonly publicView = computed(() => ['anonymous', 'public_marketplace_visitor'].includes(this.role()));
  readonly summary = computed(() => {
    if (this.publicView()) {
      return 'Public preview shows ad safety posture without exposing tenant campaign data.';
    }
    if (this.canReview()) {
      return 'Review consent, fraud, and political-content queues before delivery is approved.';
    }
    if (this.canOperateAuction()) {
      return 'Run budget-safe bids with consent and fraud gates before billing.';
    }
    return 'Control ad consent and understand why a placement was shown.';
  });

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

  setSelectedBudget(value: number): void {
    this.selectedBudget.set(value);
  }

  applyLanguage(value: Language): void {
    this.language.set(value);
    this.document.documentElement.lang = value;
    this.document.documentElement.dir = value === 'he' ? 'rtl' : 'ltr';
  }

  roleLabel(role: ViewerRole): string {
    const labels: Record<ViewerRole, string> = {
      anonymous: 'Guest preview',
      public_marketplace_visitor: 'Marketplace visitor',
      tenant_user: 'Workspace member',
      referral_user: 'Referral guest',
      freelancer: 'Creator',
      business_partner: 'Advertiser',
      tenant_admin: 'Workspace admin',
      ad_moderator: 'Ad reviewer',
      platform_admin: 'Platform admin',
      platform_support: 'Support specialist'
    };
    return labels[role];
  }

  translated(label: string): string {
    if (this.language() === 'en') {
      return label;
    }
    const labels: Record<string, string> = {
      'Advertising operations': 'תפעול פרסום',
      'Role': 'תפקיד',
      'Auction': 'מכרז',
      'Consent': 'הסכמה',
      'Review': 'בדיקה',
      'Budget available': 'תקציב זמין',
      'Consent coverage': 'כיסוי הסכמה',
      'Review queue': 'תור בדיקה',
      'Authorize bid': 'אשר הצעה',
      'Update consent': 'עדכן הסכמה',
      'Approve creative': 'אשר מודעה',
      'Preview only': 'תצוגה בלבד',
      'No private campaign data is visible here.': 'אין כאן נתוני קמפיין פרטיים.'
    };
    return labels[label] ?? label;
  }

  private isViewerRole(value: string | null): value is ViewerRole {
    return this.roles.includes(value as ViewerRole);
  }

  private isScreenState(value: string | null): value is ScreenState {
    return ['auction', 'consent', 'review'].includes(value ?? '');
  }

  private toTitle(value: string): string {
    return value
      .split(/[-_\s]+/)
      .filter(Boolean)
      .map(part => `${part.charAt(0).toUpperCase()}${part.slice(1)}`)
      .join(' ');
  }
}
