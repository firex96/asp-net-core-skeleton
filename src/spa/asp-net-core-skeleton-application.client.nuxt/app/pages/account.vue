<script setup lang="ts">
// 내 계정 — 권한과 연결된 소셜 계정. 연결은 공급자마다 폼 POST 로 시작하고, 돌아오면 ?linked= · ?error= 로 결과를 받는다.
const session = useSession()
const route = useRoute()
const providers = ref<ExternalProvider[]>([])
const logins = ref<ExternalLogin[]>([])
const error = ref<ApiError | null>(null)
const notice = ref<string | null>(null)
const linked = computed(() => new Set(logins.value.map(login => login.provider)))

useHead({ title: '내 계정' })

function queryValue(name: string): string | null {
  const value = route.query[name]
  return typeof value === 'string' ? value : null
}

onMounted(async () => {
  const user = session.user.value === undefined ? await session.refresh() : session.user.value
  if (!user) {
    await navigateTo(`/login?returnUrl=${encodeURIComponent('/account')}`)
    return
  }

  const linkedProvider = queryValue('linked')
  notice.value = linkedProvider
    ? `${providerName(linkedProvider)} 계정을 연결했어요. 이제 ${providerName(linkedProvider)}로 로그인할 수 있어요.`
    : externalLoginMessage(queryValue('error'), queryValue('provider'))
  await load()
})

async function load() {
  [providers.value, logins.value] = await Promise.all([listProviders(), listLogins()])
}

async function remove(provider: string) {
  error.value = null
  notice.value = null
  try {
    await unlink(provider)
    await load()
  }
  catch (caught) {
    error.value = caught instanceof ApiError ? caught : ApiError.unexpected()
  }
}
</script>

<template>
  <main class="card card-wide">
    <h1>내 계정</h1>
    <template v-if="session.user.value">
      <p class="lead">
        {{ session.user.value.email }}
      </p>

      <h2 class="section-title">
        권한
      </h2>
      <p v-if="session.user.value.permissions.length === 0" class="muted">
        아직 받은 권한이 없어요.
      </p>
      <ul v-else class="tags">
        <li v-for="permission in session.user.value.permissions" :key="permission" class="tag">
          <code>{{ permission }}</code>
        </li>
      </ul>

      <h2 class="section-title">
        연결된 계정
      </h2>
      <p class="muted">
        연결해 두면 비밀번호 없이 그 계정으로 로그인할 수 있어요.
      </p>
      <p v-if="notice" class="alert alert-info" role="status">
        {{ notice }}
      </p>
      <p v-if="error" class="alert alert-error" role="alert">
        {{ error.message }}
      </p>
      <ul class="list">
        <li v-for="provider in providers" :key="provider.name">
          <span>
            <strong>{{ provider.displayName }}</strong>
            <span class="muted"> · {{ linked.has(provider.name) ? '연결됨' : '연결 안 됨' }}</span>
          </span>
          <button v-if="linked.has(provider.name)" type="button" class="btn btn-small" @click="remove(provider.name)">
            연결 끊기
          </button>
          <form v-else method="post" :action="`/api/auth/external/${encodeURIComponent(provider.name)}/link`">
            <button type="submit" class="btn btn-primary btn-small">
              연결하기
            </button>
          </form>
        </li>
      </ul>
    </template>
  </main>
</template>
