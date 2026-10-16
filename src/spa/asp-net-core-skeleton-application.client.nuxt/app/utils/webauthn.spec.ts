import { describe, expect, it } from 'vitest'
import { creationOptionsFromJson, fromBase64Url, requestOptionsFromJson, serializeCredential, toBase64Url } from './webauthn'

const bytes = (...values: number[]) => new Uint8Array(values).buffer

describe('base64url', () => {
  it('round-trips bytes without padding or url-unsafe characters', () => {
    const value = bytes(251, 255, 191, 0, 1)

    const encoded = toBase64Url(value)

    expect(encoded).toBe('-_-_AAE')
    expect(new Uint8Array(fromBase64Url(encoded))).toEqual(new Uint8Array(value))
  })
})

describe('options without parse*FromJSON', () => {
  it('decodes the challenge, the user id and credential ids', () => {
    const creation = creationOptionsFromJson({
      challenge: 'AQI',
      user: { id: 'Aw', name: 'user@example.com', displayName: 'user@example.com' },
      excludeCredentials: [{ type: 'public-key', id: 'BA' }]
    })
    const request = requestOptionsFromJson({ challenge: 'AQI', rpId: 'localhost' })

    expect(new Uint8Array(creation.challenge as ArrayBuffer)).toEqual(new Uint8Array([1, 2]))
    expect(new Uint8Array(creation.user.id as ArrayBuffer)).toEqual(new Uint8Array([3]))
    expect(new Uint8Array(creation.excludeCredentials![0]!.id as ArrayBuffer)).toEqual(new Uint8Array([4]))
    expect(request.rpId).toBe('localhost')
    expect(request.allowCredentials).toEqual([])
  })
})

describe('serializeCredential', () => {
  it('uses toJSON when it carries the response', () => {
    const json = { id: 'BQ', response: { clientDataJSON: 'AQ' } }
    const credential = { toJSON: () => json } as unknown as PublicKeyCredential

    expect(serializeCredential(credential)).toBe(JSON.stringify(json))
  })

  it('encodes it directly when toJSON drops the response', () => {
    const credential = {
      id: 'BQ',
      rawId: bytes(5),
      type: 'public-key',
      toJSON: () => ({ id: 'BQ', response: {} }),
      response: { clientDataJSON: bytes(1), attestationObject: bytes(2), getTransports: () => ['internal'] }
    } as unknown as PublicKeyCredential

    expect(JSON.parse(serializeCredential(credential)).response).toEqual({
      clientDataJSON: 'AQ',
      attestationObject: 'Ag',
      transports: ['internal']
    })
  })
})

describe('serializeCredential without toJSON', () => {
  it('encodes an assertion the way the server reads it', () => {
    const credential = {
      id: 'BQ',
      rawId: bytes(5),
      type: 'public-key',
      authenticatorAttachment: 'platform',
      getClientExtensionResults: () => ({}),
      response: { clientDataJSON: bytes(1), authenticatorData: bytes(2), signature: bytes(3), userHandle: bytes(4) }
    } as unknown as PublicKeyCredential

    expect(JSON.parse(serializeCredential(credential))).toEqual({
      id: 'BQ',
      rawId: 'BQ',
      type: 'public-key',
      authenticatorAttachment: 'platform',
      clientExtensionResults: {},
      response: { clientDataJSON: 'AQ', authenticatorData: 'Ag', signature: 'Aw', userHandle: 'BA' }
    })
  })
})
