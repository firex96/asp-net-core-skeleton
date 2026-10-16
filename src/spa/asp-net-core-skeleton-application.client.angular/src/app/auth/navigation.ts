/**
 * 서버가 처리하는 주소인가 — OAuth 서버의 authorize 처럼 SPA 화면이 아닌 곳으로 돌아갈 때는 라우터가 아니라
 * 브라우저 이동(전체 페이지)으로 간다. 이 이동은 같은 사이트 안이라 SameSite=Strict 세션 쿠키가 실린다.
 */
export function isServerPath(url: string): boolean {
  return url.startsWith('/connect/') || url.startsWith('/api/');
}

/** 로그인 화면 · 계정 화면이 쿼리로 받는 결과 코드를 사람이 읽을 문장으로. */
export function externalLoginMessage(error: string | null, provider: string | null): string | null {
  const name = providerName(provider);
  switch (error) {
    case null:
      return null;
    case 'EXTERNAL_LOGIN_NOT_LINKED_ERROR':
      return `아직 연결하지 않은 ${name} 계정이에요. 이메일로 로그인한 뒤 내 계정에서 연결해 주세요.`;
    case 'EXTERNAL_LOGIN_IN_USE_ERROR':
      return `이 ${name} 계정은 이미 다른 회원과 연결돼 있어요.`;
    case 'ACCOUNT_LOCKED_ERROR':
      return '로그인에 여러 번 실패해서 계정이 잠겼어요. 30분 뒤에 다시 시도하거나 비밀번호를 재설정해 주세요.';
    default:
      return '소셜 로그인을 마치지 못했어요. 다시 시도해 주세요.';
  }
}

/** 공급자 이름(Google · Kakao · Naver)을 화면에 보일 이름으로. */
export function providerName(provider: string | null): string {
  switch (provider) {
    case 'Kakao':
      return '카카오';
    case 'Naver':
      return '네이버';
    case null:
      return '소셜';
    default:
      return provider;
  }
}
