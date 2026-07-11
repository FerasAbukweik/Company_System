import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-drop-down-menu',
  imports: [],
  templateUrl: './drop-down-menu.component.html',
  host: {
    class:
      'flex flex-col gap-1 absolute p-1 z-50 rounded-xl border border-outline-light bg-surface-lowest shadow-md',
  },
})
export class DropDownMenuComponent {
  // input
  options = input.required<string[]>();

  // output
  close = output<void>();
  selectOption = output<number>();
}
