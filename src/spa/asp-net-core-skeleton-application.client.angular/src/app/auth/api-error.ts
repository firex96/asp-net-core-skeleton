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
      return new ApiError(0, undefined, GENERIC_MESSAGE);
    }

    const problem = (typeof error.error === 'object' && error.error !== null ? error.error : {}) as ProblemDetails;
    // 검증 실패는 항목마다 문장이 온다 — 같은 문장은 한 번만(이메일 중복은 두 오류가 같은 문장이다)
    const fieldErrors = problem.errors ? [...new Set(Object.values(problem.errors).flat())] : [];
    const message = fieldErrors.length > 0 ? '입력한 내용을 확인해 주세요.' : problem.detail ?? messageOf(error.status);
    return new ApiError(error.status, problem.code, message, fieldErrors);
  }
}

const GENERIC_MESSAGE = '요청을 처리하지 못했어요. 잠시 후 다시 시도해 주세요.';

function messageOf(status: number): string {
  switch (status) {
    case 401:
      return '로그인이 필요해요.';
    case 403:
      return '이 작업을 할 권한이 없어요.';
    default:
      return GENERIC_MESSAGE;
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
