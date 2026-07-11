import { HttpInterceptorFn } from '@angular/common/http';

export const contentTypeInterceptor: HttpInterceptorFn = (req, next) => {
  const headers = req.headers;

  if (!(req.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json');
  }

  const clone = req.clone({
    headers: headers,
  });

  return next(clone);
};
