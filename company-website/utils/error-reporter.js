// 统一异常上报：POST {API_BASE}/api/logs（后端允许匿名）
// 注意：API_BASE 与 pages/index/index.vue 中的常量必须保持一致，改动时同步两处（D9）
const API_BASE = 'https://app.cssao.com'
const MAX_MESSAGE_LENGTH = 4000
const REPORT_TIMEOUT = 3000

// 防递归：上报动作自身抛错时不再触发上报
let reporting = false

function truncate(text, max) {
  if (!text) return ''
  return text.length > max ? text.slice(0, max) : text
}

// 兼容 uni-app 各端：组件异常是 Error 对象，App onError 在部分端是字符串
function toMessage(source, error, extra) {
  let detail = ''
  if (typeof error === 'string') {
    detail = error
  } else if (error && typeof error === 'object') {
    detail = `${error.name || 'Error'}: ${error.message || ''}\n${error.stack || ''}`
  } else {
    detail = String(error)
  }
  if (extra) detail += `\n${extra}`
  return truncate(`[${source}] ${detail}`, MAX_MESSAGE_LENGTH)
}

export function reportError(source, error, extra) {
  if (reporting) return
  reporting = true
  try {
    const message = toMessage(source, error, extra)
    uni.request({
      url: `${API_BASE}/api/logs`,
      method: 'POST',
      header: { 'content-type': 'application/json' },
      timeout: REPORT_TIMEOUT,
      data: { type: '异常', success: false, message },
      success: () => {}, // 静默
      fail: () => {} // 静默，不重试、不提示
    })
  } catch (e) {
    // 上报本身失败必须完全静默，且不得再次上报（无递归）
  } finally {
    // 同步复位，只挡同步重入，不阻塞后续并发上报
    reporting = false
  }
}
