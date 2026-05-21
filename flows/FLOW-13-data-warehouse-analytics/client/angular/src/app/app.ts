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
type ScreenState = 'dashboard' | 'ingest' | 'plan';

interface KpiTile {
  label: string;
  value: string;
  detail: string;
}

interface PipelineStep {
  name: string;
  owner: string;
  status: string;
}

interface MetricFact {
  source: string;
  metric: string;
  value: string;
  freshness: string;
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
  readonly moduleName = signal('Data Warehouse Analytics');
  readonly sourceFlowId = signal('FLOW-03');
  readonly eventType = signal('workflow.completed');
  readonly metricValue = signal(42);

  readonly pageTitle = computed(() => this.language() === 'he' ? 'ניתוח מחסן נתונים' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-data-warehouse-analytics`);
  readonly ingestStatus = computed(() =>
    this.metricValue() <= 0
      ? this.translated('Metric value must be positive before ingestion.')
      : this.translated('Event is ready for governed ingestion.')
  );
  readonly querySummary = computed(() => `${this.metricFacts.length} ${this.translated('metric facts returned within the tenant boundary')}`);

  readonly kpis: KpiTile[] = [
    {
      label: 'Active warehouse plans',
      value: '3',
      detail: 'Each plan covers the complete processing sequence before publication.'
    },
    {
      label: 'Raw events stored',
      value: '2',
      detail: 'Incoming events are persisted before downstream publication.'
    },
    {
      label: 'Metric facts',
      value: '2',
      detail: 'Aggregates are derived without leaking source records between tenants.'
    },
    {
      label: 'Quality checks',
      value: 'Clear',
      detail: 'Freshness, schema coverage, and lineage checks are current.'
    }
  ];

  readonly pipelineSteps: PipelineStep[] = [
    { name: 'Publish warehouse plan', owner: 'Tenant operations', status: 'Ready' },
    { name: 'Store raw event', owner: 'Ingestion service', status: 'Protected' },
    { name: 'Build metric fact', owner: 'Aggregation service', status: 'Current' },
    { name: 'Run quality check', owner: 'Warehouse monitor', status: 'Current' }
  ];

  readonly metricFacts: MetricFact[] = [
    { source: 'FLOW-03', metric: 'workflow.completed', value: '42', freshness: '2 minutes' },
    { source: 'FLOW-05', metric: 'points.awarded', value: '128', freshness: '5 minutes' },
    { source: 'FLOW-12', metric: 'revenue.normalized', value: '$13,000', freshness: '9 minutes' }
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

  setSourceFlowId(value: string): void {
    this.sourceFlowId.set(value);
  }

  setEventType(value: string): void {
    this.eventType.set(value);
  }

  setMetricValue(value: number | string): void {
    const parsed = Number(value);
    this.metricValue.set(Number.isFinite(parsed) ? parsed : 0);
  }

  applyLanguage(value: Language): void {
    this.language.set(value);
    this.document.documentElement.lang = value;
    this.document.documentElement.dir = value === 'he' ? 'rtl' : 'ltr';
  }

  roleLabel(role: ViewerRole): string {
    const labels: Record<ViewerRole, string> = {
      anonymous: 'Guest preview',
      tenant_user: 'Tenant analyst',
      referral_user: 'Referral viewer',
      freelancer: 'Freelance analyst',
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
      anonymous: 'Preview warehouse health without tenant records',
      tenant_user: 'Read analytics prepared for your tenant',
      referral_user: 'View shared campaign trends',
      freelancer: 'Inspect assigned reporting windows',
      business_partner: 'Compare governed partner metrics',
      event_organiser: 'Track event performance signals',
      tenant_admin: 'Publish plans and ingest events',
      platform_admin: 'Audit isolation and quality evidence',
      platform_support: 'Recover ingestion and query incidents'
    };
    return headings[this.role()];
  }

  roleCta(): string {
    const labels: Record<ViewerRole, string> = {
      anonymous: 'Preview dashboard',
      tenant_user: 'Open metrics',
      referral_user: 'Open shared view',
      freelancer: 'Review window',
      business_partner: 'Compare metrics',
      event_organiser: 'Inspect event health',
      tenant_admin: 'Publish plan',
      platform_admin: 'Open audit',
      platform_support: 'Review incident'
    };
    return labels[this.role()];
  }

  translated(label: string): string {
    if (this.language() === 'en') {
      return label;
    }
    const labels: Record<string, string> = {
      'Data Warehouse Analytics': 'ניתוח מחסן נתונים',
      'Dashboard': 'לוח מדדים',
      'Ingest event': 'קליטת אירוע',
      'Warehouse plan': 'תוכנית מחסן',
      'Role': 'תפקיד',
      'Source flow': 'מקור נתונים',
      'Event type': 'סוג אירוע',
      'Metric value': 'ערך מדד',
      'Metric value must be positive before ingestion.': 'ערך המדד חייב להיות חיובי לפני קליטה.',
      'Event is ready for governed ingestion.': 'האירוע מוכן לקליטה מבוקרת.',
      'Publish warehouse plan': 'פרסום תוכנית מחסן',
      'metric facts returned within the tenant boundary': 'מדדים חזרו בתוך גבול הדייר',
      'Quality check': 'בדיקת איכות',
      'Query result': 'תוצאת שאילתה'
    };
    return labels[label] ?? label;
  }

  private isViewerRole(value: string | null): value is ViewerRole {
    return this.roles.includes(value as ViewerRole);
  }

  private isScreenState(value: string | null): value is ScreenState {
    return ['dashboard', 'ingest', 'plan'].includes(value ?? '');
  }

  private toTitle(value: string): string {
    return value
      .split(/[-_\s]+/)
      .filter(Boolean)
      .map(part => `${part.charAt(0).toUpperCase()}${part.slice(1)}`)
      .join(' ');
  }
}
