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
    <header>
      <NuxtLink to="/">
        AspNetCoreSkeletonApplication
      </NuxtLink>
      <nav>
        <template v-if="session.user.value">
          <NuxtLink to="/account" class="user">
            {{ session.user.value.email }}
          </NuxtLink>
          <button type="button" @click="logOut">
            Log out
          </button>
        </template>
        <NuxtLink v-else-if="session.user.value === null" :to="loginLink">
          Log in
        </NuxtLink>
      </nav>
    </header>
    <NuxtPage />
  </div>
</template>

<style scoped>
header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.75rem 2rem;
  border-bottom: 1px solid #ddd;
}

nav {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}
</style>
