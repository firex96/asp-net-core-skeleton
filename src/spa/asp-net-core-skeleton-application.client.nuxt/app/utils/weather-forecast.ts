export interface WeatherForecast {
  date: string
  temperatureC: number
  temperatureF: number
  summary: string
}

export async function getForecasts(fetchFn: typeof fetch = fetch): Promise<WeatherForecast[]> {
  const response = await fetchFn('/weatherforecast')
  if (!response.ok) {
    throw new Error(`GET /weatherforecast failed: ${response.status}`)
  }
  return response.json()
}
