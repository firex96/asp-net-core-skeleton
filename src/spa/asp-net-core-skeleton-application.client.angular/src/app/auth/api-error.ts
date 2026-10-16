import { HttpErrorResponse } from '@angular/common/http';

/** 서버의 ProblemDetails 응답 — 메시지는 detail, 검증 실패는 errors 의 항목들이다. */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly code: string | undefined,
    message: string,
    readonly fieldErrors: string[] = []
  ) {
    super(message);
  }

  static from(error: unknown): ApiError {
    if (!(error instanceof HttpErrorResponse)) {
      return new ApiError(0, undefined, 'The request failed. Please try again.');
    }

    const problem = (typeof error.error === 'object' && error.error !== null ? error.error : {}) as ProblemDetails;
    const fieldErrors = problem.errors ? Object.values(problem.errors).flat() : [];
    const message = problem.detail ?? problem.title ?? `The request failed (${error.status}).`;
    return new ApiError(error.status, problem.code, message, fieldErrors);
  }
}

interface ProblemDetails {
  title?: string;
  detail?: string;
  code?: string;
  errors?: Record<string, string[]>;
}

/** 로그인 뒤 돌아갈 경로. 같은 출처의 경로만 허용한다 — 외부 주소로 보내는 열린 리다이렉트를 막는다. */
export function safeReturnUrl(value: string | null | undefined): string {
  if (!value || !value.startsWith('/') || value.startsWith('//') || value.startsWith('/\\')) {
    return '/';
  }
  return value;
}
