import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole = 'anonymous' | 'public_marketplace_visitor' | 'tenant_user' | 'tenant_admin' | 'freelancer' | 'business_partner' | 'platform_admin' | 'referral_user' | 'platform_support' | 'event_organiser';
type Language = 'en' | 'he';
type ScreenState = 'discover' | 'publish' | 'install' | 'settlement' | 'sandbox';

interface MarketplaceRow {
  label: string;
  state: string;
  detail: string;
}

interface MarketplaceSignal {
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
  readonly roles: ViewerRole[] = ['anonymous', 'public_marketplace_visitor', 'tenant_user', 'tenant_admin', 'freelancer', 'business_partner', 'platform_admin', 'referral_user', 'platform_support', 'event_organiser'];
  readonly role = signal<ViewerRole>('public_marketplace_visitor');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('discover');
  readonly moduleName = signal('Sharable Flows Marketplace');
  readonly readinessScore = signal(97);

  readonly packageRows: MarketplaceRow[] = [
    { label: 'Logic package', state: 'published', detail: 'flow definitions, prompts, schemas, and RAG blueprint metadata are packaged without tenant data' },
    { label: 'Install mode', state: 'linked copy', detail: 'consumer tenants install a version-pinned template and keep their own data plane private' },
    { label: 'Upgrade path', state: 'reviewed', detail: 'new upstream versions create upgrade plans with rollback rather than overwriting installed flows' }
  ];

  readonly marketplaceSignals: MarketplaceSignal[] = [
    { label: 'Artifact trust', value: 'content addressed', detail: 'published versions are immutable and require provenance evidence before listing' },
    { label: 'RAG sharing', value: 'blueprint only', detail: 'retrieval strategy and prompt schema travel, but datasets and embeddings stay private' },
    { label: 'Settlement', value: 'integer cents', detail: 'usage, installs, and affiliate payouts are attributed with bounded marketplace metrics' }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? '??? ?????? ?????' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-sharable-flows-marketplace`);
  readonly publicView = computed(() => this.role() === 'anonymous' || this.role() === 'public_marketplace_visitor' || this.role() === 'tenant_user' || this.role() === 'referral_user');
  readonly canPublish = computed(() => this.role() === 'freelancer' || this.role() === 'business_partner' || this.role() === 'platform_admin');
  readonly canInstall = computed(() => this.role() === 'tenant_admin' || this.role() === 'platform_admin');
  readonly canCurate = computed(() => this.role() === 'platform_admin');
  readonly canInspect = computed(() => this.role() === 'platform_admin' || this.role() === 'platform_support' || this.role() === 'tenant_admin');

  constructor(@Inject(DOCUMENT) private readonly documentRef: Document) {
    const params = new URLSearchParams(this.documentRef.defaultView?.location.search ?? '');
    const roleParam = params.get('role') as ViewerRole | null;
    const stateParam = params.get('state') as ScreenState | null;
    const langParam = params.get('lang') as Language | null;
    const moduleParam = params.get('module');
    if (roleParam && this.roles.includes(roleParam)) this.role.set(roleParam);
    if (stateParam && ['discover', 'publish', 'install', 'settlement', 'sandbox'].includes(stateParam)) this.screenState.set(stateParam);
    this.applyLanguage(langParam === 'he' ? 'he' : 'en');
    if (moduleParam) this.moduleName.set(moduleParam);
  }

  setRole(value: ViewerRole): void {
    this.role.set(value);
  }

  setScreenState(value: ScreenState): void {
    this.screenState.set(value);
  }

  setReadinessScore(value: number | string): void {
    this.readinessScore.set(Number(value));
  }

  applyLanguage(value: Language): void {
    this.language.set(value);
    this.documentRef.documentElement.lang = value;
    this.documentRef.documentElement.dir = value === 'he' ? 'rtl' : 'ltr';
  }

  translated(label: string): string {
    if (this.language() === 'en') return label;
    const dictionary: Record<string, string> = {
      'Marketplace workspace': '???? ???',
      'Role': '?????',
      'Discover': '?????',
      'Publish': '?????',
      'Install': '?????',
      'Settlement': '????????',
      'Sandbox': '????? ??????',
      'Packages': '??????',
      'Installs': '??????',
      'Readiness': '??????'
    };
    return dictionary[label] ?? label;
  }

  summary(): string {
    if (this.publicView()) return 'Visitors browse flow templates, ratings, version badges, install counts, and public package descriptions.';
    if (this.role() === 'tenant_admin') return 'Tenant admins install linked or snapshot copies while connector secrets and tenant data stay private.';
    if (this.role() === 'freelancer') return 'Flow authors publish logic packages and RAG blueprints for marketplace review.';
    if (this.role() === 'business_partner') return 'Business partners manage commercial template listings, support terms, and settlement visibility.';
    if (this.role() === 'platform_support') return 'Support inspectors review package disputes and install traces without mutation controls.';
    if (this.role() === 'event_organiser') return 'Event organisers can inspect compatible event-flow templates before asking an admin to install.';
    return 'Platform admins curate listings, validate package trust, run settlement checks, and provision marketplace sandboxes.';
  }

  roleLabel(value: ViewerRole): string {
    return value.replaceAll('_', ' ').replaceAll('-', ' ');
  }
}