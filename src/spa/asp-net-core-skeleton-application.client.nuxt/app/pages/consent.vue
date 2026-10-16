<script setup lang="ts">
// OAuth 동의 — 인가 서버가 /consent?…(authorize 요청 그대로) 로 보낸다. 허락 · 거절은 같은 요청을 consent=accept|deny 와 함께
// /connect/authorize 로 폼 POST 해 인가 서버가 처리한다(허락은 저장돼 다시 묻지 않는다).
const session = useSession()
const route = useRoute()
const parameters = Object.entries(route.query)
  .filter((entry): entry is [string, string] => typeof entry[1] === 'string')
const details = ref<ConsentDetails | null>(null)
const error = ref<ApiError | null>(null)

useHead({ title: 'Allow access?' })

onMounted(async () => {
  const user = session.user.value === undefined ? await session.refresh() : session.user.value
  if (!user) {
    await navigateTo(`/login?returnUrl=${encodeURIComponent(route.fullPath)}`)
    return
  }

  try {
    details.value = await getConsent(
      typeof route.query.client_id === 'string' ? route.query.client_id : '',
      typeof route.query.scope === 'string' ? route.query.scope : '',
    )
  }
  catch (caught) {
    error.value = caught instanceof ApiError ? caught : new ApiError(0, undefined, 'The request failed. Please try again.')
  }
})
</script>

<template>
  <main class="page">
    <h1>Allow access?</h1>
    <template v-if="details">
      <p><strong>{{ details.displayName }}</strong> wants to:</p>
      <ul>
        <li v-for="scope in details.scopes" :key="scope.name">
          {{ scope.description }} <code>{{ scope.name }}</code>
        </li>
      </ul>
      <form method="post" action="/connect/authorize">
        <input v-for="[name, value] in parameters" :key="name" type="hidden" :name="name" :value="value">
        <button type="submit" name="consent" value="accept">
          Allow
        </button>
        <button type="submit" name="consent" value="deny">
          Deny
        </button>
      </form>
    </template>
    <p v-if="error" class="error" role="alert">
      {{ error.message }}
    </p>
  </main>
</template>
