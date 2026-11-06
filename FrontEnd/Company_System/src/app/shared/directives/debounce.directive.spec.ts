import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { DebounceDirective } from './debounce.directive';

@Component({
  imports: [DebounceDirective],
  template: `
    <input appDebounce (appDebounce)="received.push($event)" />
    <textarea appDebounce [debounceDelay]="50" (appDebounce)="received.push($event)"></textarea>
  `,
})
class TestHost {
  received: string[] = [];
}

describe('DebounceDirective', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('emits only the latest string after 300 ms and ignores consecutive duplicates', () => {
    const fixture = TestBed.createComponent(TestHost);
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    input.value = 'Em';
    input.dispatchEvent(new Event('input'));
    vi.advanceTimersByTime(200);
    input.value = 'Employee';
    input.dispatchEvent(new Event('input'));
    vi.advanceTimersByTime(299);
    expect(fixture.componentInstance.received).toEqual([]);
    vi.advanceTimersByTime(1);
    expect(fixture.componentInstance.received).toEqual(['Employee']);
    input.dispatchEvent(new Event('input'));
    vi.advanceTimersByTime(300);
    expect(fixture.componentInstance.received).toEqual(['Employee']);
    input.value = '';
    input.dispatchEvent(new Event('input'));
    vi.advanceTimersByTime(300);
    expect(fixture.componentInstance.received).toEqual(['Employee', '']);
  });

  it('supports a custom delay and waits for text composition to finish', () => {
    const fixture = TestBed.createComponent(TestHost);
    fixture.detectChanges();
    const textarea = fixture.nativeElement.querySelector('textarea') as HTMLTextAreaElement;
    textarea.value = 'Partial';
    textarea.dispatchEvent(new InputEvent('input', { isComposing: true }));
    vi.advanceTimersByTime(100);
    expect(fixture.componentInstance.received).toEqual([]);
    textarea.value = 'Complete';
    textarea.dispatchEvent(new Event('compositionend'));
    vi.advanceTimersByTime(50);
    expect(fixture.componentInstance.received).toEqual(['Complete']);
  });

  it('cancels pending output when destroyed', () => {
    const fixture = TestBed.createComponent(TestHost);
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    input.value = 'Pending';
    input.dispatchEvent(new Event('input'));
    fixture.destroy();
    vi.advanceTimersByTime(300);
    expect(fixture.componentInstance.received).toEqual([]);
  });
});
