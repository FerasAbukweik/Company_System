import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

/**
 * Shared slide-over shell. It owns everything that is the same for every drawer in the app:
 * the slide animation, the (optional) dimmed backdrop, click-outside / Escape to close,
 * the dialog semantics and making the panel `inert` while it is closed.
 *
 * What goes INSIDE the drawer is up to each feature, via content projection:
 *
 * ```html
 * <app-drawer [open]="isOpen()" labelledBy="my-title" (closed)="close()">
 *   <h3 id="my-title">Anything you want</h3>
 *   ...
 * </app-drawer>
 * ```
 *
 * The panel is a flex column, so the projected content can be a header / scrolling body / footer.
 * The consumer stays in charge of the state: when `closed` fires, flip your own `open` flag.
 */
@Component({
  selector: 'app-drawer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (backdrop() === 'dim') {
      <div
        class="fixed inset-0 z-60 bg-corporate-blue/40 transition-opacity duration-300"
        [style.visibility]="open() ? 'visible' : 'hidden'"
        [class]="open() ? 'opacity-100' : 'opacity-0 pointer-events-none'"
        (pointerdown)="$event.target === $event.currentTarget && close()"
      ></div>
    }

    <aside
      role="dialog"
      [style.visibility]="open() ? 'visible' : 'hidden'"
      [attr.aria-hidden]="!open()"
      [attr.aria-modal]="backdrop() === 'dim'"
      [attr.aria-label]="label()"
      [attr.aria-labelledby]="labelledBy()"
      [attr.inert]="open() ? null : ''"
      [class]="panelClasses()"
    >
      <ng-content />
    </aside>
  `,
  host: {
    '(document:keydown.escape)': 'close()',
  },
})
export class DrawerComponent {
  // inputs
  /** Whether the drawer is slid in. */
  open = input.required<boolean>();
  /** `dim` darkens the page and closes on click; `none` has no overlay (the page stays usable). */
  backdrop = input<'dim' | 'none'>('dim');
  /** Accessible name when the content has no visible title. */
  label = input<string | null>(null);
  /** `id` of the element inside the drawer that titles it (preferred over `label`). */
  labelledBy = input<string | null>(null);
  /** Extra classes for the panel, mostly its width. */
  panelClass = input<string>('w-full max-w-135');

  // outputs
  /** Emitted when the user asks to close (backdrop click or Escape) while the drawer is open. */
  closed = output<void>();

  // computed
  protected readonly panelClasses = computed(
    () =>
      'fixed top-0 right-0 z-70 h-full bg-surface-lowest shadow-2xl border-l border-outline-light flex flex-col transition-transform duration-300 ease-in-out ' +
      this.panelClass() +
      (this.open() ? ' translate-x-0' : ' translate-x-full'),
  );

  // methods
  protected close() {
    if (this.open()) this.closed.emit();
  }
}
