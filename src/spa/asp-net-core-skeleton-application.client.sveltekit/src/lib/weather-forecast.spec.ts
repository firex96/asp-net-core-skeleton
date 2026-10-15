import { describe, expect, it, vi } from 'vitest';
import { getForecasts } from './weather-forecast';

describe('getForecasts', () => {
	it('should retrieve weather forecasts from the server', async () => {
		const mockForecasts = [
			{ date: '2021-10-01', temperatureC: 20, temperatureF: 68, summary: 'Mild' },
			{ date: '2021-10-02', temperatureC: 25, temperatureF: 77, summary: 'Warm' }
		];
		const fetchFn = vi.fn(async () => Response.json(mockForecasts));

		const forecasts = await getForecasts(fetchFn);

		expect(fetchFn).toHaveBeenCalledWith('/weatherforecast');
		expect(forecasts).toEqual(mockForecasts);
	});

	it('should throw when the server responds with an error', async () => {
		const fetchFn = vi.fn(async () => new Response(null, { status: 500 }));

		await expect(getForecasts(fetchFn)).rejects.toThrow('500');
	});
});
