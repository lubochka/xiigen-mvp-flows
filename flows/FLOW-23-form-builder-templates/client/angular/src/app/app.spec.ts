import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('Form Builder Templates App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [App] }).compileComponents();
  });

  it('renders the form builder templates workspace without internal protocol vocabulary', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Template operations');
    expect(text).toContain('Versioned form template definitions');
    expect(text).not.toMatch(/BFA|DNA-[1-9]|AF station|DataProcessResult|CF-[0-9]/);
  });

  it('shows gallery review posture for Hebrew RTL evidence', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.applyLanguage('he');
    component.setScreenState('gallery');
    component.setRole('template_reviewer');
    fixture.detectChanges();

    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(fixture.nativeElement.querySelector('[data-testid="gallery-page"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Schema validation');
  });

  it('keeps anonymous users on public preview copy', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.setRole('anonymous');
    fixture.detectChanges();

    expect(component.publicView()).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('preview template examples only');
  });

  it('renders submissions evidence for aggregate usage metrics', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.setScreenState('submissions');
    component.setRole('form_operator');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="submissions-page"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('PII-free aggregate usage');
    expect(fixture.nativeElement.textContent).toContain('aggregate only');
  });

  it('keeps publisher controls available for template release', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.setRole('template_publisher');
    component.setActiveFields(10);
    fixture.detectChanges();

    expect(component.canPublish()).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('10');
  });
});
