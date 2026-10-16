<script setup lang="ts">
// 로그인 두 번째 단계 — 계정의 수단(인증 앱 또는 메일 코드)으로 확인하고, 그 수단을 쓸 수 없으면 복구 코드로 한다.
// 메일 코드는 이 단계에 들어오면 한 번 보내고, 다시 받을 수 있다.
const props = defineProps<{ providers: TwoFactorProvider[] }>()
const emit = defineEmits<{ done: [], restart: [] }>()
const session = useSession()

const primary = computed<TwoFactorProvider>(() => props.providers.find(provider => provider !== 'recovery') ?? 'recovery')
const chosen = ref<TwoFactorProvider | null>(null)
const method = computed(() => chosen.value ?? primary.value)
const code = ref('')
const rememberBrowser = ref(false)
const error = ref<ApiError | null>(null)
const notice = ref<string | null>(null)
const busy = ref(false)

const lead = computed(() =>
  method.value === 'authenticator'
    ? '인증 앱에 보이는 6자리 코드를 입력해 주세요.'
    : method.value === 'email'
      ? '메일로 받은 6자리 코드를 입력해 주세요.'
      : '복구 코드 하나를 입력해 주세요. 한 번 쓴 코드는 다시 쓸 수 없어요.',
)

onMounted(() => {
  if (primary.value === 'email') {
    void sendCode(false)
  }
})

async function sendCode(again: boolean) {
  error.value = null
  try {
    await sendTwoFactorEmail()
    notice.value = again ? '확인 코드를 다시 보냈어요. 메일함을 확인해 주세요.' : '메일로 확인 코드를 보냈어요. 메일함을 확인해 주세요.'
  }
  catch (caught) {
    error.value = caught instanceof ApiError ? caught : ApiError.unexpected()
  }
}

function choose(next: TwoFactorProvider) {
  chosen.value = next
  code.value = ''
  error.value = null
  notice.value = null
}

async function submit() {
  busy.value = true
  error.value = null
  try {
    await session.finishTwoFactor(method.value, code.value, method.value !== 'recovery' && rememberBrowser.value)
    emit('done')
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
  <h1>2단계 인증</h1>
  <p class="lead">
    {{ lead }}
  </p>

  <p v-if="notice" class="alert alert-info" role="status">
    {{ notice }}
  </p>

  <form class="form" @submit.prevent="submit">
    <label class="field">
      {{ method === 'recovery' ? '복구 코드' : '확인 코드' }}
      <input v-model="code" name="code" autocomplete="one-time-code" :inputmode="method === 'recovery' ? 'text' : 'numeric'" required>
    </label>
    <label v-if="method !== 'recovery'" class="checkbox">
      <input v-model="rememberBrowser" type="checkbox">
      이 브라우저 기억하기
    </label>

    <p v-if="error" class="alert alert-error" role="alert">
      {{ error.message }}
    </p>

    <button type="submit" class="btn btn-primary btn-block" :disabled="busy">
      확인
    </button>
  </form>

  <p class="links">
    <template v-if="method === 'recovery'">
      <button v-if="primary !== 'recovery'" type="button" class="link-button" @click="choose(primary)">
        {{ primary === 'authenticator' ? '인증 앱 코드로 확인하기' : '메일 코드로 확인하기' }}
      </button>
    </template>
    <template v-else>
      <button v-if="method === 'email'" type="button" class="link-button" @click="sendCode(true)">
        코드 다시 받기
      </button>
      <button v-if="providers.includes('recovery')" type="button" class="link-button" @click="choose('recovery')">
        복구 코드로 확인하기
      </button>
    </template>
    <button type="button" class="link-button" @click="emit('restart')">
      처음부터 다시 로그인
    </button>
  </p>
</template>
