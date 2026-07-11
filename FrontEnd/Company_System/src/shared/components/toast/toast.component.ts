import { Component, inject } from '@angular/core';
import { Toast, ToastType } from './toast.model';
import { ToastService } from '../../../core/services/client/toast-service';

@Component({
  selector: 'app-toast',
  imports: [],
  templateUrl: './toast.component.html',
  styleUrl: './toast.component.css'
})
export class ToastComponent {
  readonly toastService = inject(ToastService);

  icon(type: Toast['type']): string {
    return {
      success: 'ti-circle-check',
      error: 'ti-circle-x',
      warning: 'ti-alert-triangle',
      info: 'ti-info-circle',
    }[type];
  }

  stripColor(type: ToastType): string {
    return {
      success: '#00D1B2',
      error: '#E63946',
      warning: '#F59E0B',
      info: '#3B82F6',
    }[type];
  }

  defaultTitle(type: ToastType): string {
    return { success: 'Success', error: 'Error', warning: 'Warning', info: 'Info' }[type];
  }
}
