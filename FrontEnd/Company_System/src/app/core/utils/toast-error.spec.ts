import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { ToastService } from '../services/api-services/toast-service';
import { defaultErrorReason, toastError } from './toast-error';

describe('toastError', () => {
  const title = 'تعذر تحميل المهام';
  let toast: ToastService;

  beforeEach(() => {
    toast = { error: vi.fn() } as unknown as ToastService;
  });

  it('shows the network reason and rethrows the identical error', () => {
    const failure = new HttpErrorResponse({ status: 0 });
    const onError = vi.fn();
    throwError(() => failure)
      .pipe(toastError(toast, title))
      .subscribe({ error: onError });
    expect(toast.error).toHaveBeenCalledExactlyOnceWith(defaultErrorReason(failure), title);
    expect(onError).toHaveBeenCalledWith(failure);
  });

  it('uses a custom reason for an HTTP error', () => {
    const failure = new HttpErrorResponse({ status: 403 });
    const reason = vi.fn(() => 'ليس لديك صلاحية');
    throwError(() => failure)
      .pipe(toastError(toast, title, reason))
      .subscribe({ error: () => {} });
    expect(reason).toHaveBeenCalledWith(failure);
    expect(toast.error).toHaveBeenCalledWith('ليس لديك صلاحية', title);
  });

  it('uses the unexpected-error fallback for other failures', () => {
    const failure = new Error('unexpected');
    throwError(() => failure)
      .pipe(toastError(toast, title))
      .subscribe({ error: () => {} });
    expect(toast.error).toHaveBeenCalledWith('حدث خطأ غير متوقع، حاول مرة أخرى لاحقًا', title);
  });

  it('passes successful values through without showing a toast', () => {
    const next = vi.fn();
    of(42).pipe(toastError(toast, title)).subscribe(next);
    expect(next).toHaveBeenCalledWith(42);
    expect(toast.error).not.toHaveBeenCalled();
  });
});
