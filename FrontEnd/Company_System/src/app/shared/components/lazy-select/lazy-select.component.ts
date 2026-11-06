import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  effect,
  forwardRef,
  inject,
  input,
  model,
  signal,
  untracked,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { LazySelectSource } from './lazy-select.source';
import { IsVisableDirective } from '../../directives/is-visable.directive';
import { SelectOption } from './lazy-select.model';

let nextId = 0;

/** The height the dropdown needs (max-h-60): it opens upward when there is no room below. */
const POPUP_HEIGHT = 240;

/**
 * A select that loads its options a page at a time, while the user scrolls the dropdown.
 *
 *  - lazy:   `<app-lazy-select [source]="subjectSource" [(value)]="subjectId" />`
 *            The source is any object that follows `LazySelectSource`: it gives the options and loads
 *            more of them. The first page is asked when the dropdown opens, the next ones when the end
 *            of the dropdown is reached. The select knows nothing about where the data comes from.
 *  - static: `<app-lazy-select [options]="majors" [(value)]="majorId" />`  (no source, nothing to load)
 *
 * `emptyOption` is a pinned first entry (e.g. "All subjects" / "None"). When the value is not among the
 * loaded options the select asks the source for just that item, so its name can be shown.
 * It is also a form control (`ngModel`, `formControl`). Keyboard: arrows, Home / End, Enter / Space, Esc.
 * Style the button with `triggerClass`; style the box from outside with a class on the element.
 */
@Component({
  selector: 'app-lazy-select',
  standalone: true,
  imports: [IsVisableDirective],
  templateUrl: './lazy-select.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'relative block', '(document:click)': 'onDocumentClick($event)' },
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => LazySelectComponent), multi: true },
  ],
})
export class LazySelectComponent<
  V extends string | number = string,
> implements ControlValueAccessor {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;

  /** The picked value (`''` = nothing). */
  readonly value = model<V | ''>('');
  /** A fixed list of options (a select that has no source). */
  readonly options = input<SelectOption<V>[]>([]);
  /** Lazy loading: where the options come from and how more are asked. Wins over `options`. */
  readonly source = input<LazySelectSource<V> | null>(null);
  readonly emptyOption = input<SelectOption<V | ''> | null>(null);
  /** Shown while the value matches no option. */
  readonly placeholder = input('Choose…');
  readonly disabled = input(false);
  readonly invalid = input(false);
  readonly ariaLabel = input<string | null>(null);
  /** Classes for the button (the box itself is styled with a class on the element). */
  readonly triggerClass = input('');

  protected readonly id = `app-lazy-select-${nextId++}`;
  protected readonly open = signal(false);
  protected readonly openUp = signal(false);
  protected readonly activeIndex = signal(-1);
  private readonly formDisabled = signal(false);
  protected readonly isDisabled = computed(() => this.disabled() || this.formDisabled());

  protected readonly items = computed(() => this.source()?.options() ?? this.options());
  /** What the dropdown lists: the empty option, then the items. */
  protected readonly entries = computed<SelectOption<V | ''>[]>(() => {
    const empty = this.emptyOption();
    return empty ? [empty, ...this.items()] : this.items();
  });
  protected readonly hasMore = computed(() => this.source()?.hasMore() ?? false);
  protected readonly loading = computed(() => this.source()?.loading() ?? false);
  protected readonly failed = computed(() => this.source()?.failed() ?? false);

  /** The entry of the value, or `null` while the value is empty or not loaded. */
  protected readonly selected = computed(() => {
    const value = String(this.value());
    return this.entries().find((entry) => String(entry.value) === value) ?? null;
  });

  private onChange: (value: V | '') => void = () => {};
  private onTouched: () => void = () => {};

  constructor() {
    // A value whose option was never loaded (an id from the url, the item being edited): ask for that one item.
    effect(() => {
      const value = this.value();
      const source = this.source();
      if (!source || value === '' || value == null || this.selected()) return;
      untracked(() => source.ensure(value));
    });

    effect(() => {
      if (this.isDisabled()) untracked(() => this.open.set(false));
    });
  }

  // ── Opening / closing ───────────────────────────────────────────────
  protected toggle() {
    if (this.open()) this.close();
    else this.show();
  }

  private show() {
    if (this.isDisabled()) return;
    const box = this.host.getBoundingClientRect();
    const below = window.innerHeight - box.bottom;
    this.openUp.set(below < POPUP_HEIGHT && box.top > below);

    const index = this.entries().findIndex((entry) => entry === this.selected());
    this.activeIndex.set(Math.max(index, 0));
    this.open.set(true);
    this.source()?.open();
    setTimeout(() => this.scrollToActive());
  }

  private close() {
    if (!this.open()) return;
    this.open.set(false);
    this.onTouched();
  }

  protected onDocumentClick(event: MouseEvent) {
    if (this.open() && !this.host.contains(event.target as Node)) this.close();
  }

  // ── Picking ─────────────────────────────────────────────────────────
  protected pick(entry: SelectOption<V | ''>) {
    this.value.set(entry.value);
    this.onChange(entry.value);
    this.close();
  }

  protected loadMore() {
    this.source()?.loadMore();
  }

  protected retry() {
    this.source()?.retry();
  }

  protected touch() {
    if (!this.open()) this.onTouched();
  }

  // ── Keyboard ────────────────────────────────────────────────────────
  protected onKeydown(event: KeyboardEvent) {
    const last = this.entries().length - 1;
    switch (event.key) {
      case 'ArrowDown':
      case 'ArrowUp':
        event.preventDefault();
        if (!this.open()) this.show();
        else this.moveTo(this.activeIndex() + (event.key === 'ArrowDown' ? 1 : -1));
        return;
      case 'Home':
      case 'End':
        if (!this.open()) return;
        event.preventDefault();
        this.moveTo(event.key === 'Home' ? 0 : last);
        return;
      case 'Enter':
      case ' ': {
        event.preventDefault();
        if (!this.open()) return this.show();
        const entry = this.entries()[this.activeIndex()];
        if (entry) this.pick(entry);
        else this.close();
        return;
      }
      case 'Escape':
        if (!this.open()) return;
        event.preventDefault();
        event.stopPropagation();
        this.close();
        return;
      case 'Tab':
        this.close();
        return;
    }
  }

  private moveTo(index: number) {
    const last = this.entries().length - 1;
    if (last < 0) return;
    this.activeIndex.set(Math.min(Math.max(index, 0), last));
    this.scrollToActive();
  }

  private scrollToActive() {
    this.host
      .querySelector(`#${this.id}-opt-${this.activeIndex()}`)
      ?.scrollIntoView?.({ block: 'nearest' });
  }

  // ── ControlValueAccessor ────────────────────────────────────────────
  writeValue(value: V | '' | null): void {
    this.value.set(value ?? '');
  }

  registerOnChange(fn: (value: V | '') => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(disabled: boolean): void {
    this.formDisabled.set(disabled);
  }
}
