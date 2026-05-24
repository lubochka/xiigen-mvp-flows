import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('AI Safety Moderation App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [App] }).compileComponents();
  });

  it('renders the learning safety workspace without internal protocol vocabulary', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Learning safety');
    expect(text).toContain('Every outcome waits for the learner gate');
    expect(text).not.toMatch(/BFA|DNA-[1-9]|AF station|DataProcessResult|CF-[0-9]/);
  });

  it('shows lesson review posture for Hebrew RTL evidence', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.applyLanguage('he');
    component.setScreenState('lesson');
    component.setRole('safety_reviewer');
    fixture.detectChanges();

    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(fixture.nativeElement.querySelector('[data-testid="lesson-page"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Compose, safety gate, publish');
  });

  it('keeps anonymous users on public preview copy', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.setRole('anonymous');
    fixture.detectChanges();

    expect(component.publicView()).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('Public visitors see only the safety posture');
  });

  it('renders progress evidence for server-scored outcomes', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.setScreenState('progress');
    component.setRole('educator');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="progress-page"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Server-scored progress');
    expect(fixture.nativeElement.textContent).toContain('server recomputed');
  });

  it('keeps safety reviewer controls available for moderation', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.setRole('safety_reviewer');
    component.setSafetyScore(99);
    fixture.detectChanges();

    expect(component.canReview()).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('99');
  });
});
