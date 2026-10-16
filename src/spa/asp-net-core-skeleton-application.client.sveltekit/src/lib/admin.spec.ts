import { describe, expect, it, vi } from 'vitest';
import { addMember, createRole, findUser, listGroups } from './admin';
import { ApiError } from './auth';

describe('admin api', () => {
	it('reads lists and sends role saves as JSON', async () => {
		const fetchFn = vi.fn<typeof fetch>(async () => Response.json([{ id: 1, name: 'Administrators', description: '' }]));

		expect(await listGroups(fetchFn)).toEqual([{ id: 1, name: 'Administrators', description: '' }]);

		fetchFn.mockResolvedValue(Response.json({ id: 2, name: '편집자', description: '', permissions: ['products.manage'] }, { status: 201 }));
		await createRole({ name: '편집자', description: '', permissions: ['products.manage'] }, fetchFn);
		expect(fetchFn.mock.calls[1]?.[0]).toBe('/api/authorization/roles');
		expect(fetchFn.mock.calls[1]?.[1]?.method).toBe('POST');
	});

	it('adds a member with PUT and treats 204 as done', async () => {
		const fetchFn = vi.fn<typeof fetch>(async () => new Response(null, { status: 204 }));

		await addMember(3, 7, fetchFn);

		expect(fetchFn).toHaveBeenCalledWith('/api/authorization/groups/3/members/7', expect.objectContaining({ method: 'PUT' }));
	});

	it('finds a user by email and reports an unknown one', async () => {
		const fetchFn = vi.fn<typeof fetch>(async () => Response.json({ id: 7, email: 'member@example.com' }));
		expect(await findUser('member@example.com', fetchFn)).toEqual({ userId: 7, email: 'member@example.com' });
		expect(fetchFn.mock.calls[0]?.[0]).toBe('/api/authorization/users?email=member%40example.com');

		fetchFn.mockResolvedValue(Response.json({ code: 'NOT_FOUND_ERROR', detail: '없어요' }, { status: 404 }));
		await expect(findUser('nobody@example.com', fetchFn)).rejects.toBeInstanceOf(ApiError);
	});
});
