import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole =
  | 'anonymous'
  | 'public_marketplace_visitor'
  | 'tenant_user'
  | 'tenant_admin'
  | 'freelancer'
  | 'business_partner'
  | 'event_organiser'
  | 'platform_admin'
  | 'platform_support';
type Language = 'en' | 'he';
type ScreenState = 'reader' | 'author' | 'operations';

interface ContentRow {
  title: string;
  state: string;
  guard: string;
}

interface PublishingSignal {
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
    'freelancer',
    'business_partner',
    'event_organiser',
    'platform_admin',
    'platform_support'
  ];
  readonly role = signal<ViewerRole>('tenant_admin');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('reader');
  readonly moduleName = signal('Blog/CMS Modules');
  readonly readinessScore = signal(98);

  readonly contentRows: ContentRow[] = [
    { title: 'Spring launch article', state: 'published', guard: 'search index and canonical cache updated' },
    { title: 'Partner newsletter', state: 'draft', guard: 'autosave revision retained per tenant' },
    { title: 'Community story', state: 'review', guard: 'reviewer pools cannot deadlock' }
  ];

  readonly publishingSignals: PublishingSignal[] = [
    { label: 'Revision lock', value: 'active', detail: 'conflicting updates return a clear retry state' },
    { label: 'Comment safety', value: 'guarded', detail: 'unsafe markup and oversized threads fail closed' },
    { label: 'Publishing audit', value: 'append only', detail: 'content is stored before publication events dispatch' }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? 'מודולי בלוג ותוכן' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-blog-cms-modules`);
  readonly publicView = computed(() => this.role() === 'anonymous' || this.role() === 'public_marketplace_visitor');
  readonly canAuthor = computed(() => ['tenant_user', 'tenant_admin', 'freelancer', 'business_partner', 'event_organiser', 'platform_admin'].includes(this.role()));
  readonly canConfigure = computed(() => ['tenant_admin', 'platform_admin'].includes(this.role()));
  readonly canInspect = computed(() => ['platform_support', 'tenant_admin', 'platform_admin'].includes(this.role()));
  readonly canComment = computed(() => ['anonymous', 'public_marketplace_visitor', 'tenant_user', 'tenant_admin', 'freelancer', 'business_partner', 'event_organiser'].includes(this.role()));

  constructor(@Inject(DOCUMENT) private readonly documentRef: Document) {
    const params = new URLSearchParams(this.documentRef.defaultView?.location.search ?? '');
    const roleParam = params.get('role') as ViewerRole | null;
    const stateParam = params.get('state') as ScreenState | null;
    const langParam = params.get('lang') as Language | null;
    const moduleParam = params.get('module');
    if (roleParam && this.roles.includes(roleParam)) this.role.set(roleParam);
    if (stateParam && ['reader', 'author', 'operations'].includes(stateParam)) this.screenState.set(stateParam);
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
      'Publishing workspace': 'סביבת פרסום',
      'Role': 'תפקיד',
      'Reader': 'קוראים',
      'Authoring': 'כתיבה',
      'Operations': 'תפעול',
      'Published items': 'פריטים שפורסמו',
      'Stress checks': 'בדיקות עומס',
      'Readiness': 'מוכנות'
    };
    return dictionary[label] ?? label;
  }

  summary(): string {
    if (this.publicView()) return 'Readers see public articles, safe comment entry, and no tenant-private publishing controls.';
    if (this.canConfigure()) return 'Administrators manage publishing gates, reviewer pools, route safety, and tenant-scoped search readiness.';
    if (this.canInspect()) return 'Support reviewers inspect publishing history, retained drafts, and safety outcomes without editing content.';
    if (this.canAuthor()) return 'Authors edit drafts, autosave revisions, and request publication without seeing another tenant workspace.';
    return 'This role can read and comment on published content only.';
  }

  roleLabel(value: ViewerRole): string {
    return value.replaceAll('_', ' ');
  }
}
