import { HttpErrorResponse } from '@angular/common/http';
import { catchError, MonoTypeOperatorFunction, throwError } from 'rxjs';
import { ToastService } from '../services/api-services/toast-service';

/** Fallback reason for network failures and unexpected errors. */
export function defaultErrorReason(error: unknown): string {
  return error instanceof HttpErrorResponse && error.status === 0
    ? 'تعذّر الاتصال بالخادم، تحقق من اتصالك بالإنترنت وحاول مرة أخرى'
    : 'حدث خطأ غير متوقع، حاول مرة أخرى لاحقًا';
}

/** Shows an error toast, then rethrows the same error for the caller. */
export function toastError<T>(
  toast: ToastService,
  title: string,
  reason: (error: HttpErrorResponse) => string = defaultErrorReason,
): MonoTypeOperatorFunction<T> {
  return catchError((error) => {
    // This project's ToastService accepts the message before the title.
    toast.error(reason(error), title);
    return throwError(() => error);
  });
}
