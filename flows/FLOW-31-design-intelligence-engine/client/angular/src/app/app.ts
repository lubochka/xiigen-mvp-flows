import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole = 'platform_admin' | 'platform_support' | 'tenant_admin';
type Language = 'en' | 'he';
type ScreenState = 'analysis' | 'tokens' | 'governance';

interface DesignRow {
  label: string;
  state: string;
  detail: string;
}

interface DesignSignal {
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
  readonly screenState = signal<ScreenState>('analysis');
  readonly moduleName = signal('Design Intelligence Engine');
  readonly readinessScore = signal(94);

  readonly researchRows: DesignRow[] = [
    { label: 'Figma screen', state: 'imported', detail: 'file key, screen name, components, and token evidence captured before analysis dispatch' },
    { label: 'Component map', state: 'mapped', detail: 'screen components are normalized into platform-neutral design references' },
    { label: 'Governance report', state: 'ready', detail: 'compliance evidence is retained before publication to tenant workspaces' }
  ];

  readonly ragSignals: DesignSignal[] = [
    { label: 'Token namespace', value: 'figma31', detail: 'design token records are tenant-scoped and never written to shared component catalogs' },
    { label: 'Compliance', value: 'validated', detail: 'component references are checked against the active design rule set' },
    { label: 'Timeout path', value: 'retained', detail: 'partial fabric failures keep an outbox record for retry' }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? 'מנוע בינת עיצוב' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-design-intelligence-engine`);
  readonly publicView = computed(() => false);
  readonly canAnalyze = computed(() => this.role() === 'platform_admin');
  readonly canConfigure = computed(() => this.role() === 'platform_admin');
  readonly canInspect = computed(() => this.role() === 'platform_admin' || this.role() === 'platform_support' || this.role() === 'tenant_admin');

  constructor(@Inject(DOCUMENT) private readonly documentRef: Document) {
    const params = new URLSearchParams(this.documentRef.defaultView?.location.search ?? '');
    const roleParam = params.get('role') as ViewerRole | null;
    const stateParam = params.get('state') as ScreenState | null;
    const langParam = params.get('lang') as Language | null;
    const moduleParam = params.get('module');
    if (roleParam && this.roles.includes(roleParam)) this.role.set(roleParam);
    if (stateParam && ['analysis', 'tokens', 'governance'].includes(stateParam)) this.screenState.set(stateParam);
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
      'Design workspace': 'מרחב עיצוב',
      'Role': 'תפקיד',
      'Analysis': 'ניתוח',
      'Tokens': 'טוקנים',
      'Governance': 'ממשל',
      'Design runs': 'ריצות עיצוב',
      'Compliance checks': 'בדיקות תאימות',
      'Readiness': 'מוכנות'
    };
    return dictionary[label] ?? label;
  }

  summary(): string {
    if (this.role() === 'platform_admin') return 'Platform admins analyze design files, extract tokens, validate component compliance, and publish governance reports.';
    if (this.role() === 'platform_support') return 'Support reviewers inspect design analysis and compliance timelines without mutation controls.';
    return 'Tenant admins inspect their own tenant design workspace when the tenant design console is enabled.';
  }

  roleLabel(value: ViewerRole): string {
    return value.replaceAll('_', ' ');
  }
}
