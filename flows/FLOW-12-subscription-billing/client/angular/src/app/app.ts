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
type ScreenState = 'plan' | 'subscribe' | 'dashboard';

interface PlanCard {
  id: string;
  name: string;
  price: string;
  interval: string;
  status: string;
  detail: string;
}

interface InvoiceRow {
  id: string;
  customer: string;
  amount: string;
  status: string;
}

interface MetricRow {
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
  readonly role = signal<ViewerRole>('tenant_admin');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('dashboard');
  readonly moduleName = signal('Subscription Billing');
  readonly selectedPlan = signal('growth');
  readonly paymentMethod = signal('pm_card_visa');

  readonly pageTitle = computed(() => this.language() === 'he' ? 'ניהול חיובים ומינויים' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-subscription-billing`);
  readonly activePlan = computed(() => this.planCards.find(plan => plan.id === this.selectedPlan()) ?? this.planCards[0]);
  readonly subscribeError = computed(() =>
    this.paymentMethod().startsWith('pm_declined')
      ? this.translated('Payment method needs another card before activation.')
      : this.translated('No payment issue detected.')
  );

  readonly planCards: PlanCard[] = [
    {
      id: 'starter',
      name: 'Starter workspace',
      price: '$29.00',
      interval: 'month',
      status: 'Ready',
      detail: 'Small teams get recurring billing, invoice history, and automatic retry protection.'
    },
    {
      id: 'growth',
      name: 'Growth ledger',
      price: '$99.00',
      interval: 'month',
      status: 'Popular',
      detail: 'Usage-ready subscription management with stored audit evidence before every event.'
    },
    {
      id: 'annual',
      name: 'Annual revenue',
      price: '$1,200.00',
      interval: 'year',
      status: 'Annual',
      detail: 'Annual plans normalize into monthly revenue without exposing subscriber identifiers.'
    }
  ];

  readonly invoices: InvoiceRow[] = [
    { id: 'INV-2026-0512', customer: 'Acme Studio', amount: '$99.00', status: 'Charged' },
    { id: 'INV-2026-0513', customer: 'Northwind Labs', amount: '$29.00', status: 'Retry scheduled' },
    { id: 'INV-2026-0514', customer: 'Tessera Works', amount: '$1,200.00', status: 'Annual normalized' }
  ];

  readonly metrics: MetricRow[] = [
    { label: 'Monthly revenue', value: '$13,000', detail: 'Annual and monthly plans use one normalized view.' },
    { label: 'Active subscriptions', value: '128', detail: 'Trials and active plans are visible without raw payment data.' },
    { label: 'Retry schedule', value: '2 / 5 / 9 / 14', detail: 'Charge failures show the next recovery window.' },
    { label: 'Vaulted methods', value: '100%', detail: 'Only payment method references appear in tenant records.' }
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

  setSelectedPlan(value: string): void {
    this.selectedPlan.set(value);
  }

  setPaymentMethod(value: string): void {
    this.paymentMethod.set(value);
  }

  applyLanguage(value: Language): void {
    this.language.set(value);
    this.document.documentElement.lang = value;
    this.document.documentElement.dir = value === 'he' ? 'rtl' : 'ltr';
  }

  roleLabel(role: ViewerRole): string {
    const labels: Record<ViewerRole, string> = {
      anonymous: 'Guest preview',
      tenant_user: 'Subscriber',
      referral_user: 'Referral user',
      freelancer: 'Freelancer',
      business_partner: 'Business partner',
      event_organiser: 'Event organiser',
      tenant_admin: 'Tenant admin',
      platform_admin: 'Platform admin',
      platform_support: 'Platform support'
    };
    return labels[role];
  }

  roleHeading(): string {
    const headings: Record<ViewerRole, string> = {
      anonymous: 'Preview plans without account data',
      tenant_user: 'Manage your subscription',
      referral_user: 'Confirm referred plan access',
      freelancer: 'Track workspace billing',
      business_partner: 'Review customer billing health',
      event_organiser: 'Validate event subscription status',
      tenant_admin: 'Publish and operate plans',
      platform_admin: 'Audit billing safety',
      platform_support: 'Recover failed invoice cycles'
    };
    return headings[this.role()];
  }

  roleCta(): string {
    const labels: Record<ViewerRole, string> = {
      anonymous: 'Compare plans',
      tenant_user: 'Open subscription',
      referral_user: 'Accept plan',
      freelancer: 'Review invoices',
      business_partner: 'Check billing health',
      event_organiser: 'Confirm access',
      tenant_admin: 'Publish plan',
      platform_admin: 'Open audit',
      platform_support: 'Inspect retry'
    };
    return labels[this.role()];
  }

  translated(label: string): string {
    if (this.language() === 'en') {
      return label;
    }
    const labels: Record<string, string> = {
      'Subscription Billing': 'ניהול חיובים',
      'Dashboard': 'לוח חיובים',
      'Subscribe': 'הצטרפות',
      'Plan setup': 'הגדרת מסלול',
      'Role': 'תפקיד',
      'Payment method needs another card before activation.': 'יש לעדכן אמצעי תשלום לפני הפעלה.',
      'No payment issue detected.': 'לא זוהתה בעיית תשלום.',
      'Publish plan': 'פרסום מסלול',
      'Activate subscription': 'הפעלת מנוי',
      'Billing dashboard': 'לוח חיובים',
      'Plan identifier': 'מזהה מסלול',
      'Plan name': 'שם מסלול',
      'Billing interval': 'מחזור חיוב',
      'Payment method': 'אמצעי תשלום'
    };
    return labels[label] ?? label;
  }

  private isViewerRole(value: string | null): value is ViewerRole {
    return this.roles.includes(value as ViewerRole);
  }

  private isScreenState(value: string | null): value is ScreenState {
    return ['plan', 'subscribe', 'dashboard'].includes(value ?? '');
  }

  private toTitle(value: string): string {
    return value
      .split(/[-_\s]+/)
      .filter(Boolean)
      .map(part => `${part.charAt(0).toUpperCase()}${part.slice(1)}`)
      .join(' ');
  }
}
