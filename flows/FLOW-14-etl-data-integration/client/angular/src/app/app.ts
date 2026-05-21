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

interface StagingRecord {
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
  readonly moduleName = signal('ETL Data Integration');
  readonly sourceFlowId = signal('FLOW-03');
  readonly eventType = signal('workflow.completed');
  readonly metricValue = signal(42);

  readonly pageTitle = computed(() => this.language() === 'he' ? 'אינטגרציית נתונים' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-etl-data-integration`);
  readonly ingestStatus = computed(() =>
    this.metricValue() <= 0
      ? this.translated('Metric value must be positive before ingestion.')
      : this.translated('Event is ready for governed ingestion.')
  );
  readonly querySummary = computed(() => `${this.stagingRecords.length} ${this.translated('staging records returned within the tenant boundary')}`);

  readonly kpis: KpiTile[] = [
    {
      label: 'Active ETL pipelines',
      value: '3',
      detail: 'Each pipeline covers connector setup through warehouse publication.'
    },
    {
      label: 'Raw events stored',
      value: '2',
      detail: 'Incoming events are persisted before downstream publication.'
    },
    {
      label: 'Staging records',
      value: '2',
      detail: 'Normalized records stay tenant-scoped before mart projection.'
    },
    {
      label: 'Schema drift',
      value: 'Clear',
      detail: 'Freshness, schema coverage, and lineage checks are current.'
    }
  ];

  readonly pipelineSteps: PipelineStep[] = [
    { name: 'Register connector', owner: 'Tenant operations', status: 'Ready' },
    { name: 'Store raw landing record', owner: 'Ingestion service', status: 'Protected' },
    { name: 'Normalize staging record', owner: 'Transformation service', status: 'Current' },
    { name: 'Run schema drift check', owner: 'Integration monitor', status: 'Current' }
  ];

  readonly stagingRecords: StagingRecord[] = [
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
      anonymous: 'Preview ETL readiness without tenant records',
      tenant_user: 'Read integration outputs prepared for your tenant',
      referral_user: 'View shared campaign trends',
      freelancer: 'Inspect assigned sync windows',
      business_partner: 'Compare governed partner metrics',
      event_organiser: 'Track event performance signals',
      tenant_admin: 'Publish pipelines and ingest events',
      platform_admin: 'Audit isolation and drift evidence',
      platform_support: 'Recover ingestion and query incidents'
    };
    return headings[this.role()];
  }

  roleCta(): string {
    const labels: Record<ViewerRole, string> = {
      anonymous: 'Preview pipeline',
      tenant_user: 'Open records',
      referral_user: 'Open shared view',
      freelancer: 'Review sync',
      business_partner: 'Compare metrics',
      event_organiser: 'Inspect event health',
      tenant_admin: 'Publish pipeline',
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
      'ETL Data Integration': 'אינטגרציית נתונים',
      'Dashboard': 'לוח בקרה',
      'Ingest event': 'קליטת אירוע',
      'ETL pipeline': 'צינור נתונים',
      'Role': 'תפקיד',
      'Source flow': 'זרימת מקור',
      'Event type': 'סוג אירוע',
      'Metric value': 'ערך מדד',
      'Metric value must be positive before ingestion.': 'ערך המדד חייב להיות חיובי לפני קליטה.',
      'Event is ready for governed ingestion.': 'האירוע מוכן לקליטה מבוקרת.',
      'Publish ETL pipeline': 'פרסום צינור נתונים',
      'staging records returned within the tenant boundary': 'רשומות ביניים חזרו בתוך גבול הדייר',
      'Schema drift check': 'בדיקת שינוי סכימה',
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
