import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole = 'platform_admin' | 'platform_support' | 'tenant_admin';
type Language = 'en' | 'he';
type ScreenState = 'read' | 'classify' | 'spatial' | 'write' | 'publication';

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
  readonly screenState = signal<ScreenState>('read');
  readonly moduleName = signal('Miro AI Architect');
  readonly readinessScore = signal(100);

  readonly adapterRows: AdapterRow[] = [
    { label: 'Board read', state: 'pure mapping', detail: 'Cards, shapes, connectors, text, frames, and sticky notes become shared architecture elements without storage side effects.' },
    { label: 'Shape classifier', state: 'R1-R10 deterministic', detail: 'Frame, sticky note, connector, heading, text, card, container, avatar, badge, and fallback rules expose confidence scores.' },
    { label: 'Spatial analyzer', state: '5dp tolerance', detail: 'Contained board items are grouped into row, column, or absolute layout nodes with gap and padding estimates.' },
    { label: 'Writeback', state: 'failure counted', detail: 'Shared architecture output translates to Miro payloads and writer failures increment the failed count without aborting the batch.' }
  ];

  readonly adapterSignals: AdapterSignal[] = [
    { label: 'Task types', value: '4', detail: 'board read, shape classifier, spatial analyzer, and writeback' },
    { label: 'Coverage rules', value: '9', detail: 'live mode, global approval, pricing, read, classify, spatial, writeback, package evidence, and SDK boundary' },
    { label: 'Package', value: '1.0.0', detail: '@xiigen/miro-ai-architect with STEP-1 invariants included' }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? 'אדריכל Miro AI' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-miro-shape-adapter`);
  readonly publicView = computed(() => false);
  readonly canOperate = computed(() => this.role() === 'platform_admin' || this.role() === 'tenant_admin');
  readonly canInspect = computed(() => true);

  constructor(@Inject(DOCUMENT) private readonly documentRef: Document) {
    const params = new URLSearchParams(this.documentRef.defaultView?.location.search ?? '');
    const roleParam = params.get('role') as ViewerRole | null;
    const stateParam = params.get('state') as ScreenState | null;
    const langParam = params.get('lang') as Language | null;
    const moduleParam = params.get('module');
    if (roleParam && this.roles.includes(roleParam)) this.role.set(roleParam);
    if (stateParam && ['read', 'classify', 'spatial', 'write', 'publication'].includes(stateParam)) this.screenState.set(stateParam);
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
      'Miro Shape Adapter console': 'מסוף מתאם Miro',
      'Role': 'תפקיד',
      'Language': 'שפה',
      'Read': 'קריאה',
      'Classify': 'סיווג',
      'Spatial': 'מרחב',
      'Write': 'כתיבה',
      'Publication': 'פרסום',
      'Mapped board items': 'פריטי לוח ממופים',
      'Package checks': 'בדיקות חבילה',
      'Readiness': 'מוכנות'
    };
    return dictionary[label] ?? label;
  }

  summary(): string {
    if (this.role() === 'tenant_admin') return 'Tenant admins review their Miro AI Architect package, run tenant marketplace readback, and inspect their own board mapping evidence.';
    if (this.role() === 'platform_support') return 'Platform support inspects board mapping, package evidence, spatial grouping, and write failure counts without mutation controls.';
    return 'Platform admins approve the Miro AI Architect package, verify local registry readback, and monitor deterministic board-to-architecture conversion before tenant adoption.';
  }

  roleLabel(value: ViewerRole): string {
    return value.replaceAll('_', ' ');
  }
}
