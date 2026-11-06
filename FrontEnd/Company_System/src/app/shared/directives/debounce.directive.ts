import { Directive, input, numberAttribute, output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounce, distinctUntilChanged, Subject, timer } from 'rxjs';

@Directive({
  selector: 'input[appDebounce], textarea[appDebounce]',
  host: {
    '(input)': 'onInput($event)',
    '(compositionend)': 'onInput($event)',
  },
})
export class DebounceDirective {
  readonly debounceDelay = input(300, { transform: numberAttribute });
  readonly appDebounce = output<string>();

  private readonly values = new Subject<string>();

  constructor() {
    this.values
      .pipe(
        debounce(() => timer(Math.max(0, this.debounceDelay()))),
        distinctUntilChanged(),
        takeUntilDestroyed(),
      )
      .subscribe((value) => this.appDebounce.emit(value));
  }

  protected onInput(event: Event) {
    if ((event as InputEvent).isComposing) return;
    this.values.next((event.target as HTMLInputElement | HTMLTextAreaElement).value);
  }
}
