import { Signal } from '@angular/core';
import { SelectOption } from './lazy-select.model';

/**
 * What `<app-lazy-select [source]>` reads and calls. It is only a contract: the select does not know
 * where the options come from (a service, an API, a file...). Whoever has the data implements it
 *.
 */
export interface LazySelectSource<V extends string | number = string> {
  /** The options to draw. */
  readonly options: Signal<SelectOption<V>[]>;
  readonly loading: Signal<boolean>;
  /** There are more options that were not loaded yet. */
  readonly hasMore: Signal<boolean>;
  /** The last load failed: nothing is asked again by itself, the select shows a retry button. */
  readonly failed: Signal<boolean>;
  /** The dropdown was opened: load the first page, unless it was loaded before. */
  open(): void;
  /** The end of the dropdown was reached: load the next page. */
  loadMore(): void;
  retry(): void;
  /** The value is not in the options (it was never loaded): make sure that one option exists. */
  ensure(value: V): void;
}
