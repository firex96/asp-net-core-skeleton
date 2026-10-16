<script setup lang="ts">
const forecasts = ref<WeatherForecast[]>()

useHead({ title: '날씨 예보' })

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
  <main class="container">
    <h1>날씨 예보</h1>
    <p class="lead">
      백엔드 API(<code>/weatherforecast</code>)에서 받아 온 예시 데이터예요.
    </p>

    <p v-if="!forecasts" class="muted">
      불러오는 중이에요… 백엔드가 아직 준비되지 않았다면 잠시 후 새로고침해 주세요.
    </p>
    <table v-else class="table">
      <thead>
        <tr>
          <th>날짜</th>
          <th>기온(°C)</th>
          <th>기온(°F)</th>
          <th>요약</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="forecast in forecasts" :key="forecast.date">
          <td>{{ forecast.date }}</td>
          <td class="number">
            {{ forecast.temperatureC }}
          </td>
          <td class="number">
            {{ forecast.temperatureF }}
          </td>
          <td>{{ forecast.summary }}</td>
        </tr>
      </tbody>
    </table>
  </main>
</template>
