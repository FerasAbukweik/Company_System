import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { HeroIcon } from '../../../core/constants/hero-icon-d';
import { IconComponent } from '../icon/icon.component';

/**
 * Round avatar: shows the image when there is one, otherwise the initials,
 * otherwise a generic person icon. Size it from the outside (`class="size-10"`).
 */
@Component({
  selector: 'app-avatar',
  imports: [IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (imageUrl()) {
      <img [src]="imageUrl()" [alt]="name() || 'avatar'" class="size-full object-cover" />
    } @else if (initials()) {
      <span>{{ initials() }}</span>
    } @else {
      <app-icon class="size-1/2" [path]="HeroIcon.person" />
    }
  `,
  host: {
    class:
      'inline-flex shrink-0 items-center justify-center overflow-hidden rounded-full bg-surface-high font-bold text-corporate-blue shadow-sm',
  },
})
export class AvatarComponent {
  protected readonly HeroIcon = HeroIcon;

  // inputs
  name = input<string>('');
  imageUrl = input<string | null | undefined>(null);

  // computed
  protected readonly initials = computed(() =>
    this.name()
      .trim()
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0].toUpperCase())
      .join(''),
  );
}
