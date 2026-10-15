import { defineConfig } from 'vitest/config';
import adapter from '@sveltejs/adapter-static';
import { sveltekit } from '@sveltejs/kit/vite';
import { aspNetCoreDevServer } from './aspnetcore-dev-server';

export default defineConfig(({ command }) => ({
	plugins: [
		sveltekit({
			compilerOptions: {
				// Force runes mode for the project, except for libraries. Can be removed in svelte 6.
				runes: ({ filename }) =>
					filename.split(/[/\\]/).includes('node_modules') ? undefined : true
			},
			// SPA 모드: ASP.NET Core 가 build/ 를 wwwroot 로 서빙하고, 없는 경로는 index.html 로 돌린다.
			adapter: adapter({ fallback: 'index.html' })
		})
	],
	// dev 서버(SpaProxy 가 npm start 로 띄움)에서만 HTTPS 인증서·API 프록시를 붙인다.
	server: command === 'serve' && !process.env.VITEST ? aspNetCoreDevServer() : undefined,
	test: {
		expect: { requireAssertions: true },
		projects: [
			{
				extends: './vite.config.ts',
				test: {
					name: 'server',
					environment: 'node',
					include: ['src/**/*.{test,spec}.{js,ts}'],
					exclude: ['src/**/*.svelte.{test,spec}.{js,ts}']
				}
			}
		]
	}
}));
