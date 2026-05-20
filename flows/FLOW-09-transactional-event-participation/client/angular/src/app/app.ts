import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole =
  | 'anonymous'
  | 'tenant_user'
  | 'referral_user'
  | 'freelancer'
  | 'business_partner'
  | 'event_organiser'
  | 'tenant_admin'
  | 'platform_admin'
  | 'platform_support';
type Language = 'en' | 'he';
type ScreenState = 'purchase' | 'ticket' | 'refund';

interface TicketCard {
  title: string;
  venue: string;
  price: string;
  status: string;
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
    'tenant_user',
    'referral_user',
    'freelancer',
    'business_partner',
    'event_organiser',
    'tenant_admin',
    'platform_admin',
    'platform_support'
  ];
  readonly role = signal<ViewerRole>('tenant_user');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('purchase');
  readonly moduleName = signal('Event Ticketing');

  readonly pageTitle = computed(() => this.language() === 'he' ? 'כרטיסים והשתתפות באירועים' : this.moduleName());
  readonly roleTestId = computed(() => {
    const ids: Record<ViewerRole, string> = {
      anonymous: 'role-anonymous-transactional-event-participation',
      tenant_user: 'role-tenant-user-transactional-event-participation',
      referral_user: 'role-referral-user-transactional-event-participation',
      freelancer: 'role-freelancer-transactional-event-participation',
      business_partner: 'role-business-partner-transactional-event-participation',
      event_organiser: 'role-event-organiser-transactional-event-participation',
      tenant_admin: 'role-tenant-admin-transactional-event-participation',
      platform_admin: 'role-platform-admin-transactional-event-participation',
      platform_support: 'role-platform-support-transactional-event-participation'
    };
    return ids[this.role()];
  });

  readonly ticketCards: TicketCard[] = [
    {
      title: 'Founder breakfast roundtable',
      venue: 'Harbor Loft',
      price: '$125',
      status: '4 seats left',
      detail: 'Seats are held for five minutes while payment clears, then the ticket and QR pass are issued together.',
      badge: 'Paid event'
    },
    {
      title: 'Community showcase',
      venue: 'Studio 18',
      price: '$0',
      status: 'Open entry',
      detail: 'Zero-price attendance still creates the same ticket trail, QR boundary, and attendance analytics.',
      badge: 'Free ticket'
    },
    {
      title: 'Partner launch dinner',
      venue: 'North Hall',
      price: '$480 group',
      status: 'Group booking',
      detail: 'Group purchase succeeds as one unit; if one member cannot be ticketed, every participant rolls back.',
      badge: 'Group ticket'
    }
  ];

  readonly purchaseSignals: SignalRow[] = [
    { label: 'Seat hold', value: '5 minute TTL', detail: 'Inventory is reserved before payment and released through the retry-safe flow when payment cannot clear.' },
    { label: 'Payment gate', value: 'One charge', detail: 'Repeated checkout requests reuse the cached result and never create duplicate payment records.' },
    { label: 'Fraud check', value: 'Audited fallback', detail: 'If the external fraud service is unavailable, the flow records the fallback and continues safely.' },
    { label: 'Group tickets', value: 'All or nothing', detail: 'Every group member must receive a ticket or the whole booking is rolled back.' }
  ];

  readonly ticketSignals: SignalRow[] = [
    { label: 'QR token', value: 'Platform issued', detail: 'Only the platform ticket service can mint and redeem attendance tokens.' },
    { label: 'Check-in', value: 'Single scan', detail: 'Redemption writes a tenant-scoped record and keeps personal identifiers out of aggregate analytics.' },
    { label: 'Tenant boundary', value: 'No cross-read', detail: 'Ticket lists and analytics are scoped to the current tenant context.' },
    { label: 'Outbox', value: 'Recoverable', detail: 'Timeouts keep a recovery event so queue processing can continue without losing the purchase.' }
  ];

  readonly refundSignals: SignalRow[] = [
    { label: 'Refund request', value: 'Idempotent', detail: 'Repeated refund actions with the same key reuse one response and one workflow trail.' },
    { label: 'Retry policy', value: '3 attempts', detail: 'When retries are exhausted, the flow escalates to compliance instead of silently dropping the failure.' },
    { label: 'Waitlist', value: 'Pending seat', detail: 'Sold-out events can collect a tenant-scoped pending entry for later promotion.' },
    { label: 'Analytics', value: 'Counts only', detail: 'Operational reporting uses totals and statuses, never attendee lists.' }
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
      anonymous: 'Guest visitor',
      tenant_user: 'Ticket buyer',
      referral_user: 'Referral guest',
      freelancer: 'Freelancer attendee',
      business_partner: 'Partner buyer',
      event_organiser: 'Event organiser',
      tenant_admin: 'Tenant admin',
      platform_admin: 'Platform admin',
      platform_support: 'Platform support'
    };
    return labels[role];
  }

  roleHeading(): string {
    const headings: Record<ViewerRole, string> = {
      anonymous: 'Preview available events',
      tenant_user: 'Complete a protected checkout',
      referral_user: 'Use an invitation path',
      freelancer: 'Book the working session',
      business_partner: 'Reserve partner seats',
      event_organiser: 'Manage attendance readiness',
      tenant_admin: 'Review ticket operations',
      platform_admin: 'Inspect platform controls',
      platform_support: 'Support without tenant leakage'
    };
    return headings[this.role()];
  }

  roleCta(): string {
    const ctas: Record<ViewerRole, string> = {
      anonymous: 'Sign in to book',
      tenant_user: 'Reserve seat',
      referral_user: 'Claim invitation',
      freelancer: 'Book session',
      business_partner: 'Reserve group',
      event_organiser: 'Open check-in',
      tenant_admin: 'Review orders',
      platform_admin: 'Inspect audit',
      platform_support: 'Open support view'
    };
    return ctas[this.role()];
  }

  translated(label: string): string {
    if (this.language() === 'en') {
      return label;
    }
    const labels: Record<string, string> = {
      'Event Ticketing': 'כרטיסים לאירועים',
      'Purchase': 'רכישה',
      'Ticket': 'כרטיס',
      'Refund': 'החזר',
      'Role': 'תפקיד',
      'Checkout checks': 'בדיקות רכישה',
      'Ticket and QR': 'כרטיס וקוד כניסה',
      'Refund and waitlist': 'החזר ורשימת המתנה'
    };
    return labels[label] ?? label;
  }

  private isViewerRole(value: string | null): value is ViewerRole {
    return this.roles.includes(value as ViewerRole);
  }

  private isScreenState(value: string | null): value is ScreenState {
    return ['purchase', 'ticket', 'refund'].includes(value ?? '');
  }

  private toTitle(value: string): string {
    return value
      .split(/[-_\s]+/)
      .filter(Boolean)
      .map(part => `${part.charAt(0).toUpperCase()}${part.slice(1)}`)
      .join(' ');
  }
}
