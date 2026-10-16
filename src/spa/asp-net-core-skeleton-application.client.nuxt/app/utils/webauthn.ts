/**
 * 패스키(WebAuthn) 브라우저 세리머니 — 서버(Identity)가 준 옵션 JSON 을 브라우저 API 에 넘기고, 만든 · 고른 자격 증명을 서버가 읽는
 * JSON 문자열로 돌려준다. 최신 브라우저의 parseCreationOptionsFromJSON · parseRequestOptionsFromJSON · toJSON 을 쓰고,
 * 없으면 base64url 을 직접 바꾼다.
 */
type Json = Record<string, unknown>

interface PublicKeyCredentialJsonStatics {
  parseCreationOptionsFromJSON?: (options: Json) => PublicKeyCredentialCreationOptions
  parseRequestOptionsFromJSON?: (options: Json) => PublicKeyCredentialRequestOptions
}

export function isPasskeySupported(): boolean {
  return typeof window !== 'undefined' && typeof window.PublicKeyCredential === 'function' && !!navigator.credentials
}

/** 새 패스키를 만든다(등록). 사용자가 취소하면 DOMException(NotAllowedError) 이다. */
export async function createPasskey(options: Json): Promise<string> {
  const statics = PublicKeyCredential as unknown as PublicKeyCredentialJsonStatics
  const publicKey = statics.parseCreationOptionsFromJSON?.(options) ?? creationOptionsFromJson(options)
  return serializeCredential((await navigator.credentials.create({ publicKey })) as PublicKeyCredential)
}

/** 이 사이트의 패스키 하나로 서명한다(로그인). 사용자가 취소하면 DOMException(NotAllowedError) 이다. */
export async function getPasskey(options: Json): Promise<string> {
  const statics = PublicKeyCredential as unknown as PublicKeyCredentialJsonStatics
  const publicKey = statics.parseRequestOptionsFromJSON?.(options) ?? requestOptionsFromJson(options)
  return serializeCredential((await navigator.credentials.get({ publicKey })) as PublicKeyCredential)
}

/** 사용자가 패스키 창을 닫았거나 시간이 지났는가 — 오류가 아니라 취소로 안내한다. */
export function isPasskeyCancelled(error: unknown): boolean {
  return error instanceof DOMException && (error.name === 'NotAllowedError' || error.name === 'AbortError')
}

/** 서버가 읽는 JSON 문자열. 브라우저의 toJSON 을 쓰되, 결과에 응답 본문(clientDataJSON)이 없으면(구현이 불완전하면) 직접 바꾼다. */
export function serializeCredential(credential: PublicKeyCredential): string {
  const withJson = credential as unknown as { toJSON?: () => { response?: Json } }
  const json = typeof withJson.toJSON === 'function' ? withJson.toJSON() : undefined
  if (typeof json?.response?.['clientDataJSON'] === 'string') {
    return JSON.stringify(json)
  }

  const response = credential.response as Partial<AuthenticatorAttestationResponse & AuthenticatorAssertionResponse>
  const encoded: Json = { clientDataJSON: toBase64Url(response.clientDataJSON!) }
  if (response.attestationObject) {
    encoded['attestationObject'] = toBase64Url(response.attestationObject)
    encoded['transports'] = response.getTransports?.() ?? []
  }
  if (response.authenticatorData) {
    encoded['authenticatorData'] = toBase64Url(response.authenticatorData)
    encoded['signature'] = toBase64Url(response.signature!)
    encoded['userHandle'] = response.userHandle ? toBase64Url(response.userHandle) : null
  }

  return JSON.stringify({
    id: credential.id,
    rawId: toBase64Url(credential.rawId),
    type: credential.type,
    authenticatorAttachment: credential.authenticatorAttachment ?? null,
    clientExtensionResults: credential.getClientExtensionResults?.() ?? {},
    response: encoded
  })
}

export function creationOptionsFromJson(options: Json): PublicKeyCredentialCreationOptions {
  const user = options['user'] as Json
  return {
    ...options,
    challenge: fromBase64Url(options['challenge'] as string),
    user: { ...user, id: fromBase64Url(user['id'] as string) },
    excludeCredentials: ((options['excludeCredentials'] as Json[] | undefined) ?? []).map(credential => ({
      ...credential,
      id: fromBase64Url(credential['id'] as string)
    }))
  } as unknown as PublicKeyCredentialCreationOptions
}

export function requestOptionsFromJson(options: Json): PublicKeyCredentialRequestOptions {
  return {
    ...options,
    challenge: fromBase64Url(options['challenge'] as string),
    allowCredentials: ((options['allowCredentials'] as Json[] | undefined) ?? []).map(credential => ({
      ...credential,
      id: fromBase64Url(credential['id'] as string)
    }))
  } as unknown as PublicKeyCredentialRequestOptions
}

export function toBase64Url(buffer: ArrayBuffer | ArrayBufferView): string {
  const bytes = buffer instanceof ArrayBuffer ? new Uint8Array(buffer) : new Uint8Array(buffer.buffer, buffer.byteOffset, buffer.byteLength)
  let binary = ''
  for (const byte of bytes) {
    binary += String.fromCharCode(byte)
  }
  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
}

export function fromBase64Url(value: string): ArrayBuffer {
  const base64 = value.replace(/-/g, '+').replace(/_/g, '/').padEnd(Math.ceil(value.length / 4) * 4, '=')
  const binary = atob(base64)
  const bytes = new Uint8Array(binary.length)
  for (let index = 0; index < binary.length; index++) {
    bytes[index] = binary.charCodeAt(index)
  }
  return bytes.buffer
}
