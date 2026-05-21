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
type ScreenState = 'checkout' | 'escrow' | 'admin';

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
  readonly screenState = signal<ScreenState>('checkout');
  readonly moduleName = signal('Marketplace Payments');
  readonly cartTotal = signal(128);
  readonly cardLast4 = signal('4242');
  readonly disputeReason = signal('Item not delivered');

  readonly pageTitle = computed(() => this.language() === 'he' ? 'תשלומי שוק' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-marketplace-payments`);
  readonly canOperate = computed(() => !['anonymous', 'public_visitor'].includes(this.role()));
  readonly canRelease = computed(() => ['tenant_admin', 'platform_admin', 'platform_support', 'event_organiser'].includes(this.role()));
  readonly canPayout = computed(() => ['freelancer', 'business_partner', 'event_organiser', 'tenant_admin', 'platform_admin', 'platform_support'].includes(this.role()));

  readonly checkoutRows: LedgerRow[] = [
    { label: 'Cart lock', value: 'Ready', tone: 'good' },
    { label: 'Inventory hold', value: '1 item reserved', tone: 'good' },
    { label: 'Payment split', value: 'Balanced', tone: 'good' },
    { label: 'Escrow', value: 'Held until release', tone: 'watch' }
  ];

  readonly revenueRows: LedgerRow[] = [
    { label: 'Gross volume', value: '$18,420', tone: 'neutral' },
    { label: 'Platform share', value: '$534', tone: 'good' },
    { label: 'Seller payout queue', value: '7 ready', tone: 'watch' },
    { label: 'Refund review', value: '2 pending', tone: 'watch' }
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

  setCartTotal(value: string): void {
    const parsed = Number(value);
    this.cartTotal.set(Number.isFinite(parsed) && parsed > 0 ? parsed : 0);
  }

  setDisputeReason(value: string): void {
    this.disputeReason.set(value);
  }

  applyLanguage(value: Language): void {
    this.language.set(value);
    this.document.documentElement.lang = value;
    this.document.documentElement.dir = value === 'he' ? 'rtl' : 'ltr';
  }

  roleLabel(role: ViewerRole): string {
    const labels: Record<ViewerRole, string> = {
      anonymous: 'Guest checkout',
      public_visitor: 'Public visitor',
      tenant_user: 'Buyer',
      tenant_admin: 'Store admin',
      referral_user: 'Referral viewer',
      freelancer: 'Seller',
      business_partner: 'Partner seller',
      event_organiser: 'Event organiser',
      platform_admin: 'Platform admin',
      platform_support: 'Support specialist'
    };
    return labels[role];
  }

  roleHeading(): string {
    const headings: Record<ViewerRole, string> = {
      anonymous: 'Start a guest checkout without private order history',
      public_visitor: 'Preview marketplace purchase terms',
      tenant_user: 'Review cart, payment, and escrow status',
      tenant_admin: 'Approve refunds and release held funds',
      referral_user: 'View referred checkout progress',
      freelancer: 'Track seller payout readiness',
      business_partner: 'Review partner split and payout records',
      event_organiser: 'Coordinate event order splits and seller release',
      platform_admin: 'Audit revenue, refunds, and payment safety',
      platform_support: 'Resolve stuck escrow and payout cases'
    };
    return headings[this.role()];
  }

  translated(label: string): string {
    if (this.language() === 'en') {
      return label;
    }
    const labels: Record<string, string> = {
      'Marketplace Payments': 'תשלומי שוק',
      'Role': 'תפקיד',
      'Checkout': 'תשלום',
      'Escrow': 'נאמנות',
      'Admin review': 'בדיקת מנהל',
      'Cart total': 'סכום עגלה',
      'Card ending': 'כרטיס מסתיים',
      'Release funds': 'שחרור כספים',
      'Open dispute': 'פתיחת מחלוקת',
      'Write payout': 'רישום תשלום למוכר',
      'Refund queue': 'תור החזרים'
    };
    return labels[label] ?? label;
  }

  private isViewerRole(value: string | null): value is ViewerRole {
    return this.roles.includes(value as ViewerRole);
  }

  private isScreenState(value: string | null): value is ScreenState {
    return ['checkout', 'escrow', 'admin'].includes(value ?? '');
  }

  private toTitle(value: string): string {
    return value
      .split(/[-_\s]+/)
      .filter(Boolean)
      .map(part => `${part.charAt(0).toUpperCase()}${part.slice(1)}`)
      .join(' ');
  }
}
