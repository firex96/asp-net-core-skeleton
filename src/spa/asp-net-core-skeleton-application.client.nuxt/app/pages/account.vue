<script setup lang="ts">
// 내 계정 — 권한과 연결된 소셜 계정. 연결은 공급자마다 폼 POST 로 시작하고, 돌아오면 ?linked= · ?error= 로 결과를 받는다.
const session = useSession()
const route = useRoute()
const providers = ref<ExternalProvider[]>([])
const logins = ref<ExternalLogin[]>([])
const error = ref<ApiError | null>(null)
const notice = ref<string | null>(null)
const linked = computed(() => new Set(logins.value.map(login => login.provider)))

useHead({ title: 'Account' })

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
    ? `Your ${linkedProvider} account is linked. You can now log in with it.`
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
    error.value = caught instanceof ApiError ? caught : new ApiError(0, undefined, 'The request failed. Please try again.')
  }
}
</script>

<template>
  <main class="page">
    <h1>Account</h1>
    <template v-if="session.user.value">
      <p>{{ session.user.value.email }}</p>
      <h2>Permissions</h2>
      <p v-if="session.user.value.permissions.length === 0">
        None
      </p>
      <ul v-else>
        <li v-for="permission in session.user.value.permissions" :key="permission">
          <code>{{ permission }}</code>
        </li>
      </ul>

      <h2>Linked accounts</h2>
      <p v-if="notice" class="notice" role="status">
        {{ notice }}
      </p>
      <p v-if="error" class="error" role="alert">
        {{ error.message }}
      </p>
      <ul class="providers">
        <li v-for="provider in providers" :key="provider.name">
          <span>{{ provider.displayName }}</span>
          <button v-if="linked.has(provider.name)" type="button" @click="remove(provider.name)">
            Unlink
          </button>
          <form v-else method="post" :action="`/api/auth/external/${encodeURIComponent(provider.name)}/link`">
            <button type="submit">
              Link
            </button>
          </form>
        </li>
      </ul>
    </template>
  </main>
</template>
