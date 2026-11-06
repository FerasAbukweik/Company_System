import { Injectable, signal } from '@angular/core';
import { Toast, ToastType } from '../../../shared/components/toast/toast.model';

@Injectable({ providedIn: 'root' })
export class ToastService {
  private _toasts = signal<Toast[]>([]);
  readonly toasts = this._toasts.asReadonly();

  show(message: string, type: ToastType = 'info', title: string) {
    const id = crypto.randomUUID();
    this._toasts.update((curr) => [{ id, message, type, title }, ...curr]);
    setTimeout(() => this.remove(id), 5000);
  }

  success(message: string, title?: string) {
    this.show(message, 'success', title ?? '');
  }
  error(message: string, title?: string) {
    this.show(message, 'error', title ?? '');
  }
  warning(message: string, title?: string) {
    this.show(message, 'warning', title ?? '');
  }
  info(message: string, title?: string) {
    this.show(message, 'info', title ?? '');
  }

  remove(id: string) {
    this._toasts.update((list) => list.map((t) => (t.id === id ? { ...t, removing: true } : t)));

    setTimeout(() => {
      this._toasts.update((list) => list.filter((t) => t.id !== id));
    }, 250);
  }
}
