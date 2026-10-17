<script lang="ts">
	import { onMount } from 'svelte';
	import { getForecasts, type WeatherForecast } from '#lib/weather-forecast.js';

	let forecasts = $state<WeatherForecast[]>();
	let failed = $state(false);

	onMount(async () => {
		try {
			forecasts = await getForecasts();
		} catch (error) {
			console.error(error);
			failed = true;
		}
	});
</script>

<svelte:head>
	<title>날씨 예보</title>
</svelte:head>

<main class="container">
	<h1>날씨 예보</h1>
	<p class="lead">백엔드 API(<code>/weatherforecast</code>)에서 받아 온 예시 데이터예요.</p>

	{#if failed}
		<p class="alert alert-error" role="alert">날씨 예보를 불러오지 못했어요. 잠시 후 새로고침해 주세요.</p>
	{:else if !forecasts}
		<p class="muted">불러오는 중이에요… 백엔드가 아직 준비되지 않았다면 잠시 후 새로고침해 주세요.</p>
	{:else}
		<table class="table">
			<thead>
				<tr>
					<th>날짜</th>
					<th>기온(°C)</th>
					<th>기온(°F)</th>
					<th>요약</th>
				</tr>
			</thead>
			<tbody>
				{#each forecasts as forecast (forecast.date)}
					<tr>
						<td>{forecast.date}</td>
						<td class="number">{forecast.temperatureC}</td>
						<td class="number">{forecast.temperatureF}</td>
						<td>{forecast.summary}</td>
					</tr>
				{/each}
			</tbody>
		</table>
	{/if}
</main>
