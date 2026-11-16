import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

export class ApiProblemError extends Error {
  constructor(
    message: string,
    readonly status: number,
    readonly fieldErrors: Readonly<Record<string, string[]>>
  ) {
    super(message);
    this.name = 'ApiProblemError';
  }
}

export const apiErrorInterceptor: HttpInterceptorFn = (request, next) =>
  next(request).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        return throwError(() => error);
      }

      const body = isRecord(error.error) ? error.error : {};
      const fieldErrors = readFieldErrors(readProperty(body, 'errors'));
      const message = readString(readProperty(body, 'detail')) ??
        readString(readProperty(body, 'title')) ??
        (error.status === 0
          ? 'Unable to reach the AssetFlow API. Check your connection and try again.'
          : `The request could not be completed (${error.status}).`);

      return throwError(() => new ApiProblemError(message, error.status, fieldErrors));
    })
  );

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function readString(value: unknown): string | undefined {
  return typeof value === 'string' && value.trim().length > 0 ? value : undefined;
}

function readProperty(record: Record<string, unknown>, property: string): unknown {
  const key = Object.keys(record).find(candidate => candidate.toLowerCase() === property.toLowerCase());
  return key ? record[key] : undefined;
}

function readFieldErrors(value: unknown): Readonly<Record<string, string[]>> {
  if (!isRecord(value)) {
    return {};
  }

  const errors: Record<string, string[]> = {};
  for (const [field, messages] of Object.entries(value)) {
    if (Array.isArray(messages)) {
      errors[field] = messages.filter((message): message is string => typeof message === 'string');
    }
  }
  return errors;
}
