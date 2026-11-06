import { Component, input } from '@angular/core';

@Component({
  selector: 'app-loading',
  imports: [],
  templateUrl: './loading.component.html',
})
export class LoadingComponent {
  // input
  loadingName = input.required<string>();
}
