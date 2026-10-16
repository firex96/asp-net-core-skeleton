<script setup lang="ts">
// OAuth 동의 — 인가 서버가 /consent?…(authorize 요청 그대로) 로 보낸다. 허락 · 거절은 같은 요청을 consent=accept|deny 와 함께
// /connect/authorize 로 폼 POST 해 인가 서버가 처리한다(허락은 저장돼 다시 묻지 않는다).
const session = useSession()
const route = useRoute()
const parameters = Object.entries(route.query)
  .filter((entry): entry is [string, string] => typeof entry[1] === 'string')
const details = ref<ConsentDetails | null>(null)
const error = ref<ApiError | null>(null)

useHead({ title: '접근 허용' })

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
    error.value = caught instanceof ApiError ? caught : ApiError.unexpected()
  }
})
</script>

<template>
  <main class="card">
    <template v-if="details">
      <h1>{{ details.displayName }}에 접근을 허용할까요?</h1>
      <p class="lead">
        허용하면 이 앱이 회원님 계정으로 아래 일을 할 수 있어요.
      </p>
      <ul class="list">
        <li v-for="scope in details.scopes" :key="scope.name">
          <span>{{ scope.description }}</span>
          <code class="muted">{{ scope.name }}</code>
        </li>
      </ul>
      <form method="post" action="/connect/authorize">
        <input v-for="[name, value] in parameters" :key="name" type="hidden" :name="name" :value="value">
        <div class="actions">
          <button type="submit" name="consent" value="deny" class="btn">
            거부
          </button>
          <button type="submit" name="consent" value="accept" class="btn btn-primary">
            허용
          </button>
        </div>
      </form>
      <p class="hint">
        한 번 허용하면 다음부터는 묻지 않아요.
      </p>
    </template>
    <p v-else-if="!error" class="muted">
      불러오는 중이에요…
    </p>
    <p v-if="error" class="alert alert-error" role="alert">
      {{ error.message }}
    </p>
  </main>
</template>
