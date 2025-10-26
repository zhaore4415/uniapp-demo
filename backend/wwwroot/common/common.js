// 加载公共侧边栏（使用内联HTML避免fetch请求）
function loadSidebar() {
    
    try {
        // 直接使用内联的侧边栏HTML内容
        const sidebarHtml = `
            <aside class="sidebar">
                <div class="sidebar-header">
                    <h1>AppDemo</h1>
                    <p>后台管理系统</p>
                </div>
                <ul class="nav-menu">
                    <li class="nav-item">
                        <a href="/dashboard.html" class="nav-link">
                            <span class="nav-icon">📊</span>
                            仪表盘
                        </a>
                    </li>
                    <li class="nav-item">
                        <a href="#users" class="nav-link">
                            <span class="nav-icon">👥</span>
                            用户管理
                        </a>
                    </li>
                    <li class="nav-item">
                        <a href="#products" class="nav-link">
                            <span class="nav-icon">🛍️</span>
                            产品管理
                        </a>
                    </li>
                    <li class="nav-item">
                        <a href="#orders" class="nav-link">
                            <span class="nav-icon">📋</span>
                            订单管理
                        </a>
                    </li>
                    <li class="nav-item">
                        <a href="/news-management.html" class="nav-link">
                            <span class="nav-icon">📰</span>
                            新闻管理
                        </a>
                    </li>
                    <li class="nav-item">
                        <a href="/company-content-management.html" class="nav-link">
                            <span class="nav-icon">🏢</span>
                            内容管理
                        </a>
                        <ul class="nav-submenu">
                            <li class="nav-item">
                                <a href="/company-content-management.html" class="nav-link" onclick="event.preventDefault(); editContentByKey('about')">关于我们</a>
                            </li>
                            <li class="nav-item">
                                <a href="/company-content-management.html" class="nav-link" onclick="event.preventDefault(); editContentByKey('contact')">联系我们</a>
                            </li>
                            <li class="nav-item">
                                <a href="/company-content-management.html" class="nav-link" onclick="event.preventDefault(); editContentByKey('privacy')">隐私政策</a>
                            </li>
                            <li class="nav-item">
                                <a href="/company-content-management.html" class="nav-link" onclick="event.preventDefault(); editContentByKey('terms')">服务条款</a>
                            </li>
                        </ul>
                    </li>
                    <li class="nav-divider"></li>
                    <li class="nav-item">
                        <a href="#settings" class="nav-link">
                            <span class="nav-icon">⚙️</span>
                            系统设置
                        </a>
                    </li>
                    <li class="nav-item">
                        <a href="/login.html" class="nav-link">
                            <span class="nav-icon">🚪</span>
                            退出登录
                        </a>
                    </li>
                </ul>
            </aside>
        `;
        
        // 将侧边栏插入到页面中
        const body = document.querySelector('body');
        if (body) {
            
            body.insertAdjacentHTML('afterbegin', sidebarHtml);
            
            // 根据当前页面设置活动菜单项
            setActiveMenuItem();
            
        } else {
        }
    } catch (error) {
        console.error('加载侧边栏失败:', error);
        console.error('错误详情:', error.stack);
    }
}

// 设置活动菜单项
function setActiveMenuItem() {
    const currentPath = window.location.pathname;
    const navLinks = document.querySelectorAll('.nav-link');
    
    navLinks.forEach(link => {
        // 移除所有活动状态
        link.classList.remove('active');
        
        // 获取链接的href
        const href = link.getAttribute('href');
        if (href && !href.startsWith('#') && href !== 'login.html') {
            // 检查是否匹配当前页面
            if (currentPath.includes(href)) {
                link.classList.add('active');
                
                // 为公司内容管理的子菜单添加特殊处理
                if (href === 'company-content-management.html' && link.closest('.nav-submenu')) {
                    // 激活父菜单项
                    const parentLink = link.closest('.nav-item').parentNode.previousElementSibling;
                    if (parentLink && parentLink.classList.contains('nav-link')) {
                        parentLink.classList.add('active');
                    }
                }
            }
        }
    });
}

// 检查登录状态
function checkLoginStatus() {
    // 同时检查localStorage和sessionStorage中的token
    const token = localStorage.getItem('token') || sessionStorage.getItem('token');
    const isLoginPage = window.location.pathname === '/login.html' || window.location.pathname.endsWith('/login.html');
    if (!token && !isLoginPage) {
        window.location.href = '/login.html';
    }
}

// 编辑公司内容函数（供侧边栏子菜单调用）
function editContentByKey(key) {
    // 这个函数会在公司内容管理页面的脚本中被重写
    // 这里提供一个默认实现，确保即使在其他页面点击也不会出错
    const isCompanyContentPage = window.location.pathname === '/company-content-management.html' || window.location.pathname.endsWith('/company-content-management.html');
    if (!isCompanyContentPage) {
        window.location.href = '/company-content-management.html';
        // 可以考虑使用URL参数传递key
        // window.location.href = `/company-content-management.html?key=${key}`;
    }
}

// 通用的初始化函数
function initPage() {
    checkLoginStatus();
    
    // 只在非登录页面加载侧边栏
    const isLoginPage = window.location.pathname === '/login.html' || window.location.pathname.endsWith('/login.html');
    if (!isLoginPage) {
        loadSidebar();
    }
}