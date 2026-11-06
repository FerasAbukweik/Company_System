import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Renders a Heroicons outline path (see `HeroIcon`).
 * Always size it from the outside, e.g. `<app-icon class="size-5" [path]="HeroIcon.add" />`.
 */
@Component({
  selector: 'app-icon',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <svg
      xmlns="http://www.w3.org/2000/svg"
      fill="none"
      viewBox="0 0 24 24"
      stroke-width="1.5"
      stroke="currentColor"
      class="size-full"
      aria-hidden="true"
    >
      <path stroke-linecap="round" stroke-linejoin="round" [attr.d]="path()" />
    </svg>
  `,
  host: {
    class: 'inline-flex shrink-0',
  },
})
export class IconComponent {
  path = input.required<string>();
}
