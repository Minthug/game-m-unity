import { defineConfig } from '@apps-in-toss/web-framework/config';

export default defineConfig({
  appName: 'naar',
  appType: 'game',
  brand: {
    displayName: '들어줄게',
    primaryColor: '#7C3AED',
    icon: 'https://search.pstatic.net/common/?src=http%3A%2F%2Fblogfiles.naver.net%2FMjAyNDAyMDVfMTI4%2FMDAxNzA3MTMxODYxMjg1.qgxxSfrbGWa5qkRZdhSXdFvvB2a_FRNMcZ5jy7VzxO0g.uVsEpTU_8f--1BuZOiptlrGyim5qlS53mZYkz1_VvM8g.JPEG.begoodyou1%2F%25B4%25D9%25BF%25EE%25B7%25CE%25B5%25E5%25C6%25C4%25C0%25CF%25A3%25DF20240205%25A3%25DF201710.jpg&type=l340_165',
  },
  web: {
    host: '172.30.1.21',
    port: 5173,
    commands: {
      dev: 'vite',
      build: 'vite build',
    },
  },
  webViewProps: {
    type: 'game',
    mediaPlaybackRequiresUserAction: false,
    allowsInlineMediaPlayback: true,
  },
  permissions: [],
  outdir: 'dist',
});
