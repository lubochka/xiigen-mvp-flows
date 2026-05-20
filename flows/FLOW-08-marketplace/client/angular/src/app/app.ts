import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole =
  | 'anonymous'
  | 'public_marketplace_visitor'
  | 'tenant_user'
  | 'tenant_admin'
  | 'referral_user'
  | 'freelancer'
  | 'business_partner'
  | 'event_organiser'
  | 'platform_admin'
  | 'platform_support';
type Language = 'en' | 'he';
type ScreenState = 'publish' | 'catalog' | 'analytics';

interface ListingCard {
  title: string;
  category: string;
  price: string;
  status: string;
  rating: string;
  seller: string;
  detail: string;
  badge: string;
}

interface SignalRow {
  label: string;
  value: string;
  detail: string;
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
    'tenant_admin',
    'referral_user',
    'freelancer',
    'business_partner',
    'event_organiser',
    'platform_admin',
    'platform_support'
  ];
  readonly role = signal<ViewerRole>('tenant_user');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('catalog');
  readonly moduleName = signal('Marketplace Publishing');

  readonly pageTitle = computed(() => this.language() === 'he' ? 'שוק עסקי לפרסום מוצרים' : this.moduleName());
  readonly roleTestId = computed(() => {
    const ids: Record<ViewerRole, string> = {
      anonymous: 'role-anonymous-marketplace',
      public_marketplace_visitor: 'role-public-marketplace-visitor-marketplace',
      tenant_user: 'role-tenant-user-marketplace',
      tenant_admin: 'role-tenant-admin-marketplace',
      referral_user: 'role-referral-user-marketplace',
      freelancer: 'role-freelancer-marketplace',
      business_partner: 'role-business-partner-marketplace',
      event_organiser: 'role-event-organiser-marketplace',
      platform_admin: 'role-platform-admin-marketplace',
      platform_support: 'role-platform-support-marketplace'
    };
    return ids[this.role()];
  });

  readonly listings: ListingCard[] = [
    {
      title: 'Launch copy sprint',
      category: 'Services',
      price: '$1,200',
      status: 'Active',
      rating: '4.9',
      seller: 'North Star Studio',
      detail: 'A verified seller package for product pages, email launch copy, and buyer-ready descriptions.',
      badge: 'Service listing'
    },
    {
      title: 'Retail bundle photography',
      category: 'Creative',
      price: '$0',
      status: 'Free sample',
      rating: '4.7',
      seller: 'Bright Cart Co.',
      detail: 'Zero-price listings stay visible as free discovery offers while still passing moderation.',
      badge: 'Free listing'
    },
    {
      title: 'Partner launch kit',
      category: 'Partnerships',
      price: '$3,800',
      status: 'Review',
      rating: '4.8',
      seller: 'Atlas Partners',
      detail: 'Cooperator-ready offer with audience overlap, referral terms, and bundle suggestions.',
      badge: 'Partnership opportunity'
    }
  ];

  readonly publishSignals: SignalRow[] = [
    { label: 'Audit first', value: 'Saved before checks', detail: 'The seller action is recorded before moderation, price checks, listing storage, or feed work.' },
    { label: 'Moderation', value: 'Draft on reject', detail: 'Rejected content becomes a draft instead of a failed publication.' },
    { label: 'Pricing', value: '$0 allowed', detail: 'Free listings are valid; negative prices are blocked with field-level feedback.' },
    { label: 'Retry safety', value: 'One listing', detail: 'Repeated publish requests reuse the cached response and do not duplicate catalog rows.' }
  ];

  readonly analyticsRows: SignalRow[] = [
    { label: 'Conversion', value: '4.0', detail: 'Inquiries divide by views, using one when views are zero.' },
    { label: 'Feed event', value: 'Count only', detail: 'Feed publication shares aggregate counts without buyer or viewer identifiers.' },
    { label: 'Catalog', value: '3 listings', detail: 'Listings are scoped to the current tenant catalog.' },
    { label: 'Outbox', value: 'Recoverable', detail: 'Timeouts keep a recovery event so queue processing can retry.' }
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

  applyLanguage(value: Language): void {
    this.language.set(value);
    this.document.documentElement.lang = value;
    this.document.documentElement.dir = value === 'he' ? 'rtl' : 'ltr';
  }

  roleLabel(role: ViewerRole): string {
    const labels: Record<ViewerRole, string> = {
      anonymous: 'Anonymous visitor',
      public_marketplace_visitor: 'Marketplace visitor',
      tenant_user: 'Tenant buyer',
      tenant_admin: 'Seller admin',
      referral_user: 'Referral buyer',
      freelancer: 'Freelancer seller',
      business_partner: 'Business partner',
      event_organiser: 'Event organiser',
      platform_admin: 'Platform admin',
      platform_support: 'Platform support'
    };
    return labels[role];
  }

  roleHeading(): string {
    const headings: Record<ViewerRole, string> = {
      anonymous: 'Browse trusted offers',
      public_marketplace_visitor: 'Shared listing preview',
      tenant_user: 'Buyer marketplace',
      tenant_admin: 'Seller operations',
      referral_user: 'Referral offer path',
      freelancer: 'Service seller shelf',
      business_partner: 'Cooperator opportunities',
      event_organiser: 'Event-ready offers',
      platform_admin: 'Marketplace governance',
      platform_support: 'Support-safe inspection'
    };
    return headings[this.role()];
  }

  roleCta(): string {
    const ctas: Record<ViewerRole, string> = {
      anonymous: 'Sign in to save',
      public_marketplace_visitor: 'View shared offer',
      tenant_user: 'Contact seller',
      tenant_admin: 'Publish listing',
      referral_user: 'Claim referral price',
      freelancer: 'Post a service',
      business_partner: 'Explore partnership',
      event_organiser: 'Add to event plan',
      platform_admin: 'Review marketplace',
      platform_support: 'Open support view'
    };
    return ctas[this.role()];
  }

  translated(label: string): string {
    if (this.language() === 'en') {
      return label;
    }
    const labels: Record<string, string> = {
      'Marketplace': 'שוק עסקי',
      'Publish': 'פרסום',
      'Catalog': 'קטלוג',
      'Analytics': 'מדדים',
      'Role': 'תפקיד',
      'Listing checks': 'בדיקות פרסום',
      'Catalog feed': 'עדכון קטלוג',
      'Marketplace health': 'בריאות השוק'
    };
    return labels[label] ?? label;
  }

  private isViewerRole(value: string | null): value is ViewerRole {
    return this.roles.includes(value as ViewerRole);
  }

  private isScreenState(value: string | null): value is ScreenState {
    return ['publish', 'catalog', 'analytics'].includes(value ?? '');
  }

  private toTitle(value: string): string {
    return value
      .split(/[-_\s]+/)
      .filter(Boolean)
      .map(part => `${part.charAt(0).toUpperCase()}${part.slice(1)}`)
      .join(' ');
  }
}
