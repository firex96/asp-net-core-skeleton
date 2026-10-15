<script lang="ts">
	import { onMount } from 'svelte';
	import { getForecasts, type WeatherForecast } from '#lib/weather-forecast.js';

	let forecasts = $state<WeatherForecast[]>();

	onMount(async () => {
		try {
			forecasts = await getForecasts();
		} catch (error) {
			console.error(error);
		}
	});
</script>

<svelte:head>
	<title>AspNetCoreSkeletonApplication</title>
</svelte:head>

<main>
	<h1 id="tableLabel">Weather forecast</h1>

	<p>This component demonstrates fetching data from the server.</p>

	{#if !forecasts}
		<p><em>Loading... Please refresh once the ASP.NET backend has started.</em></p>
	{:else}
		<table>
			<thead>
				<tr>
					<th>Date</th>
					<th>Temp. (C)</th>
					<th>Temp. (F)</th>
					<th>Summary</th>
				</tr>
			</thead>
			<tbody>
				{#each forecasts as forecast (forecast.date)}
					<tr>
						<td>{forecast.date}</td>
						<td>{forecast.temperatureC}</td>
						<td>{forecast.temperatureF}</td>
						<td>{forecast.summary}</td>
					</tr>
				{/each}
			</tbody>
		</table>
	{/if}
</main>

<style>
	main {
		max-width: 1280px;
		padding: 2rem;
		margin: 0 auto;
		text-align: center;
	}

	th,
	td {
		padding-left: 1rem;
		padding-right: 1rem;
	}

	table {
		margin: 0 auto;
	}
</style>
