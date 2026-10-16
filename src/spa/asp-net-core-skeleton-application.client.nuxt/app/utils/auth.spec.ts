import { describe, expect, it, vi } from 'vitest'
import { ApiError, getCurrentUser, login, logout, register, safeReturnUrl } from './auth'

const user = { id: 7, email: 'user@example.com', permissions: [] }

describe('getCurrentUser', () => {
  it('returns the signed-in user', async () => {
    const fetchFn = vi.fn(async () => Response.json(user))

    expect(await getCurrentUser(fetchFn)).toEqual(user)
    expect(fetchFn).toHaveBeenCalledWith('/api/auth/me')
  })

  it('treats 401 as signed out', async () => {
    const fetchFn = vi.fn(async () => new Response(null, { status: 401 }))

    expect(await getCurrentUser(fetchFn)).toBeNull()
  })
})

describe('login', () => {
  it('posts the credentials as JSON', async () => {
    const fetchFn = vi.fn(async () => new Response(null, { status: 204 }))

    await login(user.email, 'Passw0rd!', fetchFn)

    expect(fetchFn).toHaveBeenCalledWith('/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: user.email, password: 'Passw0rd!' }),
    })
  })

  it('throws ApiError with the server message and code', async () => {
    const fetchFn = vi.fn(async () =>
      Response.json({ status: 423, code: 'ACCOUNT_LOCKED_ERROR', detail: 'locked' }, { status: 423 }),
    )

    const error = await login(user.email, 'wrong', fetchFn).catch((caught: unknown) => caught)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ status: 423, code: 'ACCOUNT_LOCKED_ERROR', message: 'locked' })
  })
})

describe('register', () => {
  it('logs in after creating the account', async () => {
    const fetchFn = vi.fn<typeof fetch>(async () => new Response(null, { status: 201 }))

    await register(user.email, 'Passw0rd!', fetchFn)

    expect(fetchFn.mock.calls.map(([url]) => url)).toEqual(['/api/auth/register', '/api/auth/login'])
  })

  it('collects validation messages from errors', async () => {
    const fetchFn = vi.fn(async () =>
      Response.json(
        { title: 'One or more validation errors occurred.', errors: { PasswordTooShort: ['too short'], PasswordRequiresDigit: ['digit'] } },
        { status: 400 },
      ),
    )

    const error = (await register(user.email, 'short', fetchFn).catch((caught: unknown) => caught)) as ApiError

    expect(error.message).toBe('One or more validation errors occurred.')
    expect(error.fieldErrors).toEqual(['too short', 'digit'])
    expect(fetchFn).toHaveBeenCalledTimes(1)
  })
})

describe('logout', () => {
  it('posts to the logout endpoint', async () => {
    const fetchFn = vi.fn(async () => new Response(null, { status: 204 }))

    await logout(fetchFn)

    expect(fetchFn).toHaveBeenCalledWith('/api/auth/logout', { method: 'POST', headers: undefined, body: undefined })
  })
})

describe('safeReturnUrl', () => {
  it.each([
    ['/products?page=2', '/products?page=2'],
    [null, '/'],
    ['', '/'],
    ['https://evil.example', '/'],
    ['//evil.example', '/'],
    ['/\\evil.example', '/'],
  ])('maps %s to %s', (value, expected) => {
    expect(safeReturnUrl(value)).toBe(expected)
  })
})
