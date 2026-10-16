/**
 * 서버가 처리하는 주소인가 — OAuth 서버의 authorize 처럼 SPA 화면이 아닌 곳으로 돌아갈 때는 라우터가 아니라
 * 브라우저 이동(전체 페이지)으로 간다. 이 이동은 같은 사이트 안이라 SameSite=Strict 세션 쿠키가 실린다.
 */
export function isServerPath(url: string): boolean {
  return url.startsWith('/connect/') || url.startsWith('/api/');
}

/** 로그인 화면 · 계정 화면이 쿼리로 받는 결과 코드를 사람이 읽을 문장으로. */
export function externalLoginMessage(error: string | null, provider: string | null): string | null {
  const name = provider ?? 'social';
  switch (error) {
    case null:
      return null;
    case 'EXTERNAL_LOGIN_NOT_LINKED_ERROR':
      return `Your ${name} account is not linked yet. Log in with your password, then link it on the Account page.`;
    case 'EXTERNAL_LOGIN_IN_USE_ERROR':
      return `That ${name} account is already linked to another user.`;
    case 'ACCOUNT_LOCKED_ERROR':
      return 'Your account is locked after too many failed attempts. Try again later.';
    default:
      return 'Social login failed. Please try again.';
  }
}
