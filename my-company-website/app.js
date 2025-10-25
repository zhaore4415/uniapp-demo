const express = require('express')
const app = express()

// ✅ 加在这里：CORS 中间件
app.use((req, res, next) => {
  res.header('Access-Control-Allow-Origin', '*')           // 允许任意域名访问
  res.header('Access-Control-Allow-Methods', 'GET,PUT,POST,DELETE,OPTIONS')
  res.header('Access-Control-Allow-Headers', 'Content-Type, Authorization')
  if (req.method === 'OPTIONS') {
    res.sendStatus(200)
  } else {
    next()
  }
})

// 解析 JSON 请求体
app.use(express.json())

// 示例接口
app.get('/api/company/info', (req, res) => {
  res.json({
    code: 200,
    data: {
      title: '我的企业',
      description: '我们是一家专注于科技创新的公司...',
      phone: '138-0000-0000',
      email: 'contact@company.com',
      address: '北京市朝阳区XXX大厦',
      services: [
        { name: '网站建设', desc: '专业响应式网站设计' },
        { name: '小程序开发', desc: '微信/支付宝小程序定制' }
      ]
    }
  })
})

// 启动服务器
const PORT = 5000
app.listen(PORT, '0.0.0.0', () => {
  console.log(`Server is running at http://192.168.1.10:${PORT}`)
})