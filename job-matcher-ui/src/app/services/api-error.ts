import { HttpErrorResponse } from '@angular/common/http';

/** RFC 7807 body the API returns for every error. */
export interface ProblemDetails {
  title?: string;
  status?: number;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
  ) {
    super(message);
  }
}

/** User-facing message for an error response; status 0 means the request never reached the server. */
export function errorMessage(status: number, problem: ProblemDetails | null | undefined, fallback: string): string {
  if (status === 0) return 'Cannot reach the server. Please try again later.';
  if (status === 429) return 'Too many requests. Please wait a bit and try again.';

  const validationError = problem?.errors ? Object.values(problem.errors).flat()[0] : undefined;
  return validationError ?? problem?.title ?? fallback;
}

export function toApiError(err: unknown, fallback: string): ApiError {
  if (err instanceof ApiError) return err;
  if (err instanceof HttpErrorResponse) {
    return new ApiError(err.status, errorMessage(err.status, err.error, fallback));
  }
  // fetch() throws a TypeError when the network request fails
  return new ApiError(0, errorMessage(0, null, fallback));
}
