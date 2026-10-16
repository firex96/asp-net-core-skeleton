<script setup lang="ts">
// 로그인과 가입을 한 화면에서 전환한다. 성공하면 returnUrl(같은 출처 경로만)로 돌아간다 — OAuth 서버의 authorize 같은
// 서버 주소면 브라우저 이동으로 간다. 이미 로그인돼 있으면 곧바로 돌아간다. 소셜 로그인은 공급자마다 폼 POST 로 시작한다(연결된 계정만).
const session = useSession()
const route = useRoute()

const mode = ref<'login' | 'register'>('login')
const email = ref('')
const password = ref('')
const confirmPassword = ref('')
const error = ref<ApiError | null>(null)
const busy = ref(false)
const providers = ref<ExternalProvider[]>([])

const returnUrl = computed(() => safeReturnUrl(typeof route.query.returnUrl === 'string' ? route.query.returnUrl : null))
const title = computed(() => (mode.value === 'login' ? '로그인' : '회원가입'))
const notice = computed(() =>
  externalLoginMessage(queryValue('error'), queryValue('provider'))
  ?? (queryValue('reset') === 'done' ? '비밀번호를 바꿨어요. 새 비밀번호로 로그인해 주세요.' : null),
)

function queryValue(name: string): string | null {
  const value = route.query[name]
  return typeof value === 'string' ? value : null
}

function externalLoginAction(provider: string): string {
  return `/api/auth/external/${encodeURIComponent(provider)}/login?returnUrl=${encodeURIComponent(returnUrl.value)}`
}

async function continueTo(target: string) {
  if (isServerPath(target)) {
    window.location.assign(target)
    return
  }

  await navigateTo(target)
}

useHead({ title })

onMounted(async () => {
  const user = session.user.value === undefined ? await session.refresh() : session.user.value
  if (user) {
    await continueTo(returnUrl.value)
    return
  }

  providers.value = await listProviders().catch(() => [])
})

function toggleMode() {
  mode.value = mode.value === 'login' ? 'register' : 'login'
  error.value = null
}

async function submit() {
  if (mode.value === 'register' && password.value !== confirmPassword.value) {
    error.value = new ApiError(400, undefined, '비밀번호가 서로 달라요. 다시 입력해 주세요.')
    return
  }

  busy.value = true
  error.value = null
  try {
    await (mode.value === 'login' ? session.signIn(email.value, password.value) : session.signUp(email.value, password.value))
    await continueTo(returnUrl.value)
  }
  catch (caught) {
    error.value = caught instanceof ApiError ? caught : ApiError.unexpected()
  }
  finally {
    busy.value = false
  }
}
</script>

<template>
  <main class="card">
    <h1>{{ title }}</h1>
    <p class="lead">
      {{ mode === 'login' ? '이메일과 비밀번호로 로그인해 주세요.' : '이메일로 새 계정을 만들어요.' }}
    </p>

    <p v-if="notice" class="alert alert-info" role="status">
      {{ notice }}
    </p>

    <form class="form" @submit.prevent="submit">
      <label class="field">
        이메일
        <input v-model="email" name="email" type="email" autocomplete="username" placeholder="name@example.com" required>
      </label>
      <label class="field">
        비밀번호
        <input
          v-model="password"
          name="password"
          type="password"
          :autocomplete="mode === 'login' ? 'current-password' : 'new-password'"
          required
        >
      </label>
      <template v-if="mode === 'register'">
        <label class="field">
          비밀번호 확인
          <input v-model="confirmPassword" name="confirmPassword" type="password" autocomplete="new-password" required>
        </label>
        <p class="hint">
          10자 이상, 영문 대문자 · 소문자와 숫자, 특수 문자를 섞어 주세요.
        </p>
      </template>

      <div v-if="error" class="alert alert-error" role="alert">
        <p>{{ error.message }}</p>
        <ul v-if="error.fieldErrors.length > 0">
          <li v-for="message in error.fieldErrors" :key="message">
            {{ message }}
          </li>
        </ul>
      </div>

      <button type="submit" class="btn btn-primary btn-block" :disabled="busy">
        {{ mode === 'login' ? '로그인' : '가입하기' }}
      </button>
    </form>

    <template v-if="mode === 'login'">
      <template v-if="providers.length > 0">
        <div class="divider">
          또는
        </div>
        <div class="social">
          <form v-for="provider in providers" :key="provider.name" method="post" :action="externalLoginAction(provider.name)">
            <button type="submit" :class="`btn btn-block btn-${provider.name.toLowerCase()}`">
              {{ provider.displayName }}로 계속하기
            </button>
          </form>
        </div>
      </template>

      <p class="links">
        <NuxtLink to="/forgot-password">
          비밀번호를 잊으셨나요?
        </NuxtLink>
        <button type="button" class="link-button" @click="toggleMode">
          회원가입
        </button>
      </p>
    </template>
    <p v-else class="links">
      <span class="muted">이미 계정이 있나요?</span>
      <button type="button" class="link-button" @click="toggleMode">
        로그인
      </button>
    </p>
  </main>
</template>
