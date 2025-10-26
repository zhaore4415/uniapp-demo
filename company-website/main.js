// main.js - 第一行开始，必须是第一行！

// 只在 App 和 小程序端初始化 global.UTS
if (process.env.UNI_PLATFORM === 'app' || process.env.UNI_PLATFORM.startsWith('mp-')) {
  // 兼容 global 不存在的情况（如某些小程序环境）
  if (typeof global === 'undefined') {
    window.global = window; // 或 self/globalThis
  }
  // 挂载 UTS 全局对象（仅在需要的平台）
  global.UTS = global.UTS || {};
  global.UTSJSONObject = JSON;
}

import App from './App'

// #ifndef VUE3
import Vue from 'vue'
import './uni.promisify.adaptor'
Vue.config.productionTip = false
App.mpType = 'app'
const app = new Vue({
  ...App
})
app.$mount()
// #endif

// #ifdef VUE3
import { createSSRApp } from 'vue'
export function createApp() {
  const app = createSSRApp(App)
  return {
    app
  }
}
// #endif