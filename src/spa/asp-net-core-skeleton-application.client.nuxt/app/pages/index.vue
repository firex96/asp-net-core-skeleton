<script setup lang="ts">
const forecasts = ref<WeatherForecast[]>()

onMounted(async () => {
  try {
    forecasts.value = await getForecasts()
  }
  catch (error) {
    console.error(error)
  }
})
</script>

<template>
  <main>
    <h1 id="tableLabel">
      Weather forecast
    </h1>

    <p>This component demonstrates fetching data from the server.</p>

    <p v-if="!forecasts">
      <em>Loading... Please refresh once the ASP.NET backend has started.</em>
    </p>
    <table v-else>
      <thead>
        <tr>
          <th>Date</th>
          <th>Temp. (C)</th>
          <th>Temp. (F)</th>
          <th>Summary</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="forecast in forecasts" :key="forecast.date">
          <td>{{ forecast.date }}</td>
          <td>{{ forecast.temperatureC }}</td>
          <td>{{ forecast.temperatureF }}</td>
          <td>{{ forecast.summary }}</td>
        </tr>
      </tbody>
    </table>
  </main>
</template>

<style scoped>
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
