import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole = 'platform_admin' | 'platform_support' | 'tenant_admin';
type Language = 'en' | 'he';
type ScreenState = 'shorthands' | 'routing' | 'diff' | 'package' | 'publication';

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
  readonly roles: ViewerRole[] = ['platform_admin', 'platform_support', 'tenant_admin'];
  readonly role = signal<ViewerRole>('platform_admin');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('shorthands');
  readonly moduleName = signal('Webflow Designer Bridge');
  readonly readinessScore = signal(100);

  readonly adapterRows: AdapterRow[] = [
    { label: 'CSS shorthands', state: 'expanded', detail: 'Border, font, padding, margin, and shadow values become discrete CSS properties before they enter the pipeline.' },
    { label: 'Element routing', state: 'all categories covered', detail: 'Layout, text, link, button, media, form, navigation, tab, slider, dropdown, lightbox, symbol, commerce, embed, and map elements route to non-empty output.' },
    { label: 'Unknown CSS', state: 'preserved', detail: 'Unknown properties are passed through after shorthand expansion so Webflow-specific tokens are not dropped.' },
    { label: 'Validation diff', state: 'structured', detail: 'Pipeline-only, native-only, and value-difference buckets compare generated CSS against native Webflow CSS.' }
  ];

  readonly adapterSignals: AdapterSignal[] = [
    { label: 'Element types', value: '50+', detail: 'advanced Designer and commerce categories are included' },
    { label: 'Coverage rules', value: '7', detail: 'shorthands, routing, SDK boundary, diff, package, live auth, and scope' },
    { label: 'Package', value: '1.0.0', detail: '@xiigen/webflow-designer-extension with STEP-1 invariants included' }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? 'גשר Webflow Designer' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-webflow-designer-extension`);
  readonly canOperate = computed(() => this.role() === 'platform_admin' || this.role() === 'tenant_admin');
  readonly canInspect = computed(() => true);

  constructor(@Inject(DOCUMENT) private readonly documentRef: Document) {
    const params = new URLSearchParams(this.documentRef.defaultView?.location.search ?? '');
    const roleParam = params.get('role') as ViewerRole | null;
    const stateParam = params.get('state') as ScreenState | null;
    const langParam = params.get('lang') as Language | null;
    const moduleParam = params.get('module');
    if (roleParam && this.roles.includes(roleParam)) this.role.set(roleParam);
    if (stateParam && ['shorthands', 'routing', 'diff', 'package', 'publication'].includes(stateParam)) this.screenState.set(stateParam);
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
      'Webflow Designer Extension console': 'מסוף הרחבת Webflow Designer',
      'Role': 'תפקיד',
      'Language': 'שפה',
      'Shorthands': 'קיצורי CSS',
      'Routing': 'ניתוב',
      'Diff': 'השוואה',
      'Package': 'חבילה',
      'Publication': 'פרסום',
      'Mapped elements': 'אלמנטים ממופים',
      'Package checks': 'בדיקות חבילה',
      'Readiness': 'מוכנות'
    };
    return dictionary[label] ?? label;
  }

  summary(): string {
    if (this.role() === 'tenant_admin') return 'Tenant admins review their Webflow Designer package, run marketplace readback, and inspect CSS routing evidence before adoption.';
    if (this.role() === 'platform_support') return 'Platform support inspects shorthand expansion, package evidence, validation diff counts, and tenant-scoped run records without mutation controls.';
    return 'Platform admins approve the Webflow Designer Extension package, verify local registry readback, and monitor CSS parity before tenant adoption.';
  }

  roleLabel(value: ViewerRole): string {
    return value.replaceAll('_', ' ');
  }
}
