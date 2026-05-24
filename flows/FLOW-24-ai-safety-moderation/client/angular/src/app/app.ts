import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole =
  | 'anonymous'
  | 'student'
  | 'parent_guardian'
  | 'educator'
  | 'safety_reviewer'
  | 'content_author'
  | 'tenant_admin'
  | 'platform_admin';
type Language = 'en' | 'he';
type ScreenState = 'consent' | 'lesson' | 'progress';

interface LessonRow {
  topic: string;
  safety: 'tokened' | 'review' | 'blocked';
  source: string;
}

interface GateRow {
  gate: string;
  outcome: string;
  effect: string;
}

interface ProgressRow {
  signal: string;
  value: string;
  guard: string;
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
    'student',
    'parent_guardian',
    'educator',
    'safety_reviewer',
    'content_author',
    'tenant_admin',
    'platform_admin'
  ];
  readonly role = signal<ViewerRole>('student');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('consent');
  readonly moduleName = signal('AI Safety Moderation');
  readonly safetyScore = signal(96);

  readonly gates: GateRow[] = [
    { gate: 'Learning consent', outcome: 'granted', effect: 'AI lesson flow allowed' },
    { gate: 'Safety review', outcome: 'token issued', effect: 'content can publish' },
    { gate: 'Calendar connector', outcome: 'fabric path', effect: 'no direct provider SDK' }
  ];

  readonly lessons: LessonRow[] = [
    { topic: 'Fractions practice', safety: 'tokened', source: 'compose -> safety gate -> publish' },
    { topic: 'Essay coaching', safety: 'review', source: 'waiting for safety reviewer' },
    { topic: 'Unsafe prompt', safety: 'blocked', source: 'not published' }
  ];

  readonly progressRows: ProgressRow[] = [
    { signal: 'Quiz score', value: '50%', guard: 'server recomputed' },
    { signal: 'Streak timezone', value: 'UTC', guard: 'profile source only' },
    { signal: 'Domain pack', value: 'skipped', guard: 'missing config skips safely' }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? 'בטיחות למידה חכמה' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-ai-safety-moderation`);
  readonly publicView = computed(() => this.role() === 'anonymous');
  readonly canLearn = computed(() => ['student', 'parent_guardian', 'educator', 'tenant_admin', 'platform_admin'].includes(this.role()));
  readonly canAuthor = computed(() => ['content_author', 'educator', 'tenant_admin', 'platform_admin'].includes(this.role()));
  readonly canReview = computed(() => ['safety_reviewer', 'tenant_admin', 'platform_admin'].includes(this.role()));
  readonly canOperate = computed(() => ['educator', 'tenant_admin', 'platform_admin'].includes(this.role()));

  constructor(@Inject(DOCUMENT) private readonly documentRef: Document) {
    const params = new URLSearchParams(this.documentRef.defaultView?.location.search ?? '');
    const roleParam = params.get('role') as ViewerRole | null;
    const stateParam = params.get('state') as ScreenState | null;
    const langParam = params.get('lang') as Language | null;
    const moduleParam = params.get('module');
    if (roleParam && this.roles.includes(roleParam)) this.role.set(roleParam);
    if (stateParam && ['consent', 'lesson', 'progress'].includes(stateParam)) this.screenState.set(stateParam);
    this.applyLanguage(langParam === 'he' ? 'he' : 'en');
    if (moduleParam) this.moduleName.set(moduleParam);
  }

  setRole(value: ViewerRole): void {
    this.role.set(value);
  }

  setScreenState(value: ScreenState): void {
    this.screenState.set(value);
  }

  setSafetyScore(value: number | string): void {
    this.safetyScore.set(Number(value));
  }

  applyLanguage(value: Language): void {
    this.language.set(value);
    this.documentRef.documentElement.lang = value;
    this.documentRef.documentElement.dir = value === 'he' ? 'rtl' : 'ltr';
  }

  translated(label: string): string {
    if (this.language() === 'en') return label;
    const dictionary: Record<string, string> = {
      'Learning safety': 'בטיחות למידה',
      'Role': 'תפקיד',
      'Consent': 'הסכמה',
      'Lesson': 'שיעור',
      'Progress': 'התקדמות',
      'Open consent gates': 'שערי הסכמה',
      'Published lessons': 'שיעורים שפורסמו',
      'Safety score': 'ציון בטיחות'
    };
    return dictionary[label] ?? label;
  }

  summary(): string {
    if (this.publicView()) return 'Public visitors see only the safety posture and cannot access learner records.';
    if (this.canReview()) return 'Safety reviewers approve learning content only after the moderation token is issued.';
    if (this.canAuthor()) return 'Content authors compose lessons, then wait for the safety gate before publication.';
    if (this.canOperate()) return 'Educators schedule lessons, sync calendars through connectors, and monitor consent status.';
    if (this.canLearn()) return 'Learners and guardians see consent, server-graded progress, and safe lesson availability.';
    return 'This role has read-only visibility into the learning safety posture.';
  }

  roleLabel(value: ViewerRole): string {
    return value.replaceAll('_', ' ');
  }
}
