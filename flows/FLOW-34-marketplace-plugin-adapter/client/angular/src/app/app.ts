import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole = 'anonymous' | 'public_marketplace_visitor' | 'tenant_user' | 'tenant_admin' | 'business_partner' | 'platform_admin' | 'freelancer' | 'platform_support';
type Language = 'en' | 'he';
type ScreenState = 'planning' | 'registry' | 'validation' | 'package' | 'audit';

interface AdapterRow {
  label: string;
  state: string;
  detail: string;
}

interface AdapterSignal {
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
  readonly roles: ViewerRole[] = ['anonymous', 'public_marketplace_visitor', 'tenant_user', 'tenant_admin', 'business_partner', 'platform_admin', 'freelancer', 'platform_support'];
  readonly role = signal<ViewerRole>('platform_admin');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('planning');
  readonly moduleName = signal('Marketplace Plugin Adapter');
  readonly readinessScore = signal(96);

  readonly adapterRows: AdapterRow[] = [
    { label: 'Porting decision', state: 'approved', detail: 'each marketplace platform declares whether it can run as a thin adapter before any code is generated' },
    { label: 'Adapter surface', state: 'mapped', detail: 'the API map, platform registry entry, and package manifest are checked as one versioned envelope' },
    { label: 'Marketplace package', state: 'published', detail: 'validated adapter bundles are published to the local marketplace registry with tenant-scoped evidence' }
  ];

  readonly adapterSignals: AdapterSignal[] = [
    { label: 'Thin adapter', value: 'mode b', detail: 'adapter packages contain platform glue only and no tenant business rules' },
    { label: 'Validation', value: 'gated', detail: 'porting, secrets, direct HTTP, and package manifest checks must pass before listing' },
    { label: 'Outbox', value: 'retained', detail: 'timeout and partial-failure paths keep retry evidence for the worker' }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? 'מתאם תוספי מרקטפלייס' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-marketplace-plugin-adapter`);
  readonly publicView = computed(() => this.role() === 'anonymous' || this.role() === 'public_marketplace_visitor');
  readonly canOperate = computed(() => this.role() === 'tenant_admin' || this.role() === 'business_partner' || this.role() === 'platform_admin');
  readonly canInspect = computed(() => this.role() !== 'anonymous');

  constructor(@Inject(DOCUMENT) private readonly documentRef: Document) {
    const params = new URLSearchParams(this.documentRef.defaultView?.location.search ?? '');
    const roleParam = params.get('role') as ViewerRole | null;
    const stateParam = params.get('state') as ScreenState | null;
    const langParam = params.get('lang') as Language | null;
    const moduleParam = params.get('module');
    if (roleParam && this.roles.includes(roleParam)) this.role.set(roleParam);
    if (stateParam && ['planning', 'registry', 'validation', 'package', 'audit'].includes(stateParam)) this.screenState.set(stateParam);
    this.applyLanguage(langParam === 'he' ? 'he' : 'en');
    if (moduleParam) this.moduleName.set(moduleParam);
  }

  setRole(value: ViewerRole): void {
    this.role.set(value);
  }

  setScreenState(value: ScreenState): void {
    this.screenState.set(value);
  }

  applyLanguage(value: Language): void {
    this.language.set(value);
    this.documentRef.documentElement.lang = value;
    this.documentRef.documentElement.dir = value === 'he' ? 'rtl' : 'ltr';
  }

  translated(label: string): string {
    if (this.language() === 'en') return label;
    const dictionary: Record<string, string> = {
      'Adapter control room': 'חדר בקרת מתאמים',
      'Role': 'תפקיד',
      'Planning': 'תכנון',
      'Registry': 'רישום',
      'Validation': 'אימות',
      'Package': 'חבילה',
      'Audit': 'ביקורת',
      'Runs': 'הרצות',
      'Platforms': 'פלטפורמות',
      'Readiness': 'מוכנות'
    };
    return dictionary[label] ?? label;
  }

  summary(): string {
    if (this.publicView()) return 'Public marketplace visitors can inspect listing readiness without mutating adapter packages.';
    if (this.role() === 'platform_support') return 'Platform support can inspect adapter registry imports, validation gaps, package evidence, and audit records without publishing.';
    if (this.role() === 'tenant_user' || this.role() === 'freelancer') return 'Tenant users and freelancers can preview approved marketplace adapters before installation.';
    return 'Operators approve porting decisions, validate thin adapter packages, and publish tenant-safe listings to the marketplace.';
  }

  roleLabel(value: ViewerRole): string {
    return value.replaceAll('_', ' ');
  }
}
