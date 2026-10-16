<script setup lang="ts">
const session = useSession()
const route = useRoute()

// 계정 화면들(로그인 · 비밀번호 찾기 · 재설정)에서는 돌아올 곳을 싣지 않는다 — 재설정 주소의 일회용 코드를 다른 주소로 옮기지 않는다.
const loginLink = computed(() =>
  ['/login', '/forgot-password', '/reset-password'].includes(route.path)
    ? { path: '/login' }
    : { path: '/login', query: { returnUrl: route.fullPath } },
)

onMounted(() => {
  session.refresh().catch(error => console.error(error))
})

async function logOut() {
  await session.signOut()
  await navigateTo('/')
}
</script>

<template>
  <div>
    <NuxtRouteAnnouncer />
    <header class="app-header">
      <NuxtLink to="/" class="brand">
        AspNetCoreSkeletonApplication
      </NuxtLink>
      <nav class="nav">
        <template v-if="session.user.value">
          <NuxtLink to="/account">
            {{ session.user.value.email }}
          </NuxtLink>
          <button type="button" class="btn btn-small" @click="logOut">
            로그아웃
          </button>
        </template>
        <NuxtLink v-else-if="session.user.value === null && route.path !== '/login'" :to="loginLink" class="btn btn-primary btn-small">
          로그인
        </NuxtLink>
      </nav>
    </header>
    <NuxtPage />
  </div>
</template>
