import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole = 'platform_admin' | 'platform_support' | 'tenant_admin';
type Language = 'en' | 'he';
type ScreenState = 'tiers' | 'shadow' | 'signals' | 'progress';

interface FeedbackRow {
  label: string;
  state: string;
  detail: string;
}

interface FeedbackSignal {
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
  readonly screenState = signal<ScreenState>('tiers');
  readonly moduleName = signal('OSS Curriculum');
  readonly readinessScore = signal(91);

  readonly feedbackRows: FeedbackRow[] = [
    { label: 'Curriculum tier', state: 'assigned', detail: 'Validated DPO triples receive a required tier from the fixed archetype map before validation completes.' },
    { label: 'Shadow runs', state: 'queued', detail: 'Winning nodes are sent to configured OSS models asynchronously with model and cycle traceability.' },
    { label: 'Progress plateau', state: 'recommended', detail: 'Static grades for three cycles recommend pre-seeding without automatic model swaps.' }
  ];

  readonly feedbackSignals: FeedbackSignal[] = [
    { label: 'Tier map', value: '1-5', detail: 'ROUTING through SCHEDULED archetypes map to machine-fixed curriculum tiers' },
    { label: 'Signals', value: '3', detail: 'grade trend, RAG context size, and graph context size are the only stored signals' },
    { label: 'Broker', value: 'stubbed', detail: 'queue dispatch is tested locally after outbox append and ready for a real broker swap' }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? 'תוכנית לימוד קוד פתוח' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-oss-curriculum`);
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
    if (stateParam && ['tiers', 'shadow', 'signals', 'progress'].includes(stateParam)) this.screenState.set(stateParam);
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
      'Learning feedback console': '×ž×¡×•×£ ×ž×©×•×‘ ×œ×ž×™×“×”',
      'Role': '×ª×¤×§×™×“',
      'Tiers': '×¨×ž×•×ª',
      'Shadow': '×”×¨×¦×ª ×¦×œ',
      'Progress': '×”×ª×§×“×ž×•×ª',
      'Signals': '××•×ª×•×ª',
      'Cycles reviewed': '×ž×—×–×•×¨×™× ×©× ×‘×“×§×•',
      'Pattern updates': '×¢×“×›×•× ×™ ×ª×‘× ×™×ª',
      'Readiness': '×ž×•×›× ×•×ª'
    };
    return dictionary[label] ?? label;
  }

  summary(): string {
    if (this.role() === 'tenant_admin') return 'Tenant admins can assign curriculum tiers, submit OSS shadow runs, and collect learning signals for their tenant.';
    if (this.role() === 'platform_support') return 'Platform support can inspect curriculum tiers, shadow run traceability, and retained timeout evidence without mutation controls.';
    return 'Platform admins manage OSS curriculum tiering, shadow comparisons, learning-signal collection, and plateau recommendations across tenants.';
  }

  roleLabel(value: ViewerRole): string {
    return value.replaceAll('_', ' ');
  }
}

