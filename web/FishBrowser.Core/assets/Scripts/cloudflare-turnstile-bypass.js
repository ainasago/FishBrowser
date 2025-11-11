// ============================================================================
// Cloudflare Turnstile 终极绕过脚本
// 针对 Error 600010 的完整解决方案
// ============================================================================

(function() {
    'use strict';
    
    console.log('[Turnstile Bypass] 🚀 Initializing comprehensive bypass...');
    
    // ============================================================================
    // 第 0 部分：修复 Vendor 与 Platform 的一致性（最关键！）
    // ============================================================================
    
    try {
        const currentPlatform = navigator.platform;
        const currentVendor = navigator.vendor;
        
        // 检查 vendor 是否与 platform 匹配
        const expectedVendor = (currentPlatform === 'iPhone' || currentPlatform === 'iPad' || currentPlatform === 'iPod' || currentPlatform === 'MacIntel') 
            ? 'Apple Computer, Inc.' 
            : 'Google Inc.';
        
        if (currentVendor !== expectedVendor) {
            console.warn('[Turnstile Bypass] ⚠️ Vendor mismatch detected!');
            console.warn(`  Platform: ${currentPlatform}`);
            console.warn(`  Current Vendor: ${currentVendor}`);
            console.warn(`  Expected Vendor: ${expectedVendor}`);
            
            // 强制修复 vendor
            Object.defineProperty(navigator, 'vendor', {
                get: () => expectedVendor,
                configurable: true
            });
            
            console.log(`[Turnstile Bypass] ✅ Vendor fixed: ${currentVendor} -> ${expectedVendor}`);
        } else {
            console.log(`[Turnstile Bypass] ✅ Vendor matches platform: ${currentPlatform} -> ${currentVendor}`);
        }
    } catch (e) {
        console.warn('[Turnstile Bypass] ⚠️ Vendor fix failed:', e);
    }
    
    // ============================================================================
    // 第 1 部分：移除所有自动化痕迹
    // ============================================================================
    
    // 1.1 完全移除 webdriver
    try {
        delete Object.getPrototypeOf(navigator).webdriver;
        delete navigator.__proto__.webdriver;
        delete navigator.webdriver;
        
        Object.defineProperty(navigator, 'webdriver', {
            get: () => undefined,
            configurable: true,
            enumerable: false
        });
        
        console.log('[Turnstile Bypass] ✅ webdriver removed');
    } catch (e) {
        console.warn('[Turnstile Bypass] ⚠️ webdriver removal failed:', e);
    }
    
    // 1.2 移除 Selenium/Puppeteer/Playwright 痕迹
    const automationProps = [
        '__webdriver_script_fn',
        '__driver_evaluate',
        '__webdriver_evaluate',
        '__selenium_evaluate',
        '__fxdriver_evaluate',
        '__driver_unwrapped',
        '__webdriver_unwrapped',
        '__selenium_unwrapped',
        '__fxdriver_unwrapped',
        '_Selenium_IDE_Recorder',
        '_selenium',
        'calledSelenium',
        '$cdc_asdjflasutopfhvcZLmcfl_',
        '$chrome_asyncScriptInfo',
        '__$webdriverAsyncExecutor',
        '__lastWatirAlert',
        '__lastWatirConfirm',
        '__lastWatirPrompt',
        '__webdriver_script_func',
        '_WEBDRIVER_ELEM_CACHE',
        'ChromeDriverw',
        'driver-evaluate',
        'webdriver-evaluate',
        'selenium-evaluate',
        'webdriverCommand',
        'webdriver-evaluate-response',
        '__webdriverFunc',
        '__webdriver_script_function',
        '__playwright',
        '__pw_manual',
        '__PW_inspect'
    ];
    
    automationProps.forEach(prop => {
        try {
            delete window[prop];
            delete document[prop];
        } catch (e) {}
    });
    
    console.log('[Turnstile Bypass] ✅ Automation traces removed');
    
    // 1.3 清除 CDP Runtime 痕迹
    if (window.chrome && window.chrome.runtime) {
        try {
            const originalRuntime = window.chrome.runtime;
            delete window.chrome.runtime;
            
            // 重新定义为空对象（模拟真实 Chrome）
            Object.defineProperty(window.chrome, 'runtime', {
                get: () => undefined,
                configurable: true
            });
            
            console.log('[Turnstile Bypass] ✅ CDP Runtime cleared');
        } catch (e) {}
    }
    
    // ============================================================================
    // 第 2 部分：修复 Permissions API（Cloudflare 会检查）
    // ============================================================================
    
    if (navigator.permissions && navigator.permissions.query) {
        const originalQuery = navigator.permissions.query;
        
        navigator.permissions.query = function(parameters) {
            // 对于 notifications 权限，返回真实的状态
            if (parameters.name === 'notifications') {
                return Promise.resolve({
                    state: 'default',
                    onchange: null
                });
            }
            
            // 其他权限正常处理
            return originalQuery.apply(this, arguments);
        };
        
        console.log('[Turnstile Bypass] ✅ Permissions API patched');
    }
    
    // ============================================================================
    // 第 3 部分：修复 Chrome 对象（关键！）
    // ============================================================================
    
    if (!window.chrome) {
        window.chrome = {};
    }
    
    // 3.1 添加 chrome.app（真实 Chrome 有这个）
    if (!window.chrome.app) {
        window.chrome.app = {
            isInstalled: false,
            InstallState: {
                DISABLED: 'disabled',
                INSTALLED: 'installed',
                NOT_INSTALLED: 'not_installed'
            },
            RunningState: {
                CANNOT_RUN: 'cannot_run',
                READY_TO_RUN: 'ready_to_run',
                RUNNING: 'running'
            }
        };
    }
    
    // 3.2 添加 chrome.csi（真实 Chrome 有这个）
    if (!window.chrome.csi) {
        window.chrome.csi = function() {
            return {
                startE: Date.now(),
                onloadT: Date.now(),
                pageT: Math.random() * 1000,
                tran: 15
            };
        };
    }
    
    // 3.3 添加 chrome.loadTimes（真实 Chrome 有这个）
    if (!window.chrome.loadTimes) {
        window.chrome.loadTimes = function() {
            return {
                requestTime: Date.now() / 1000,
                startLoadTime: Date.now() / 1000,
                commitLoadTime: Date.now() / 1000,
                finishDocumentLoadTime: Date.now() / 1000,
                finishLoadTime: Date.now() / 1000,
                firstPaintTime: Date.now() / 1000,
                firstPaintAfterLoadTime: 0,
                navigationType: 'Other',
                wasFetchedViaSpdy: false,
                wasNpnNegotiated: true,
                npnNegotiatedProtocol: 'h2',
                wasAlternateProtocolAvailable: false,
                connectionInfo: 'h2'
            };
        };
    }
    
    console.log('[Turnstile Bypass] ✅ Chrome object enhanced');
    
    // ============================================================================
    // 第 4 部分：修复 Plugin 检测（Cloudflare 会验证）
    // ============================================================================
    
    // 确保 plugins 和 mimeTypes 返回真实的 PDF 插件
    try {
        const originalPlugins = navigator.plugins;
        const originalLength = originalPlugins.length;
        
        // 如果已经有 plugins，不要修改（避免 length 只读错误）
        if (originalLength > 0) {
            console.log('[Turnstile Bypass] ✅ Plugins already exist, skipping fix');
        } else {
            // 只在没有 plugins 时才修复
            const pdfPlugin = {
                0: { type: 'application/pdf', suffixes: 'pdf', description: 'Portable Document Format' },
                1: { type: 'text/pdf', suffixes: 'pdf', description: 'Portable Document Format' },
                description: 'Portable Document Format',
                filename: 'internal-pdf-viewer',
                length: 2,
                name: 'PDF Viewer'
            };
            
            // 使用 Proxy 代替直接修改 PluginArray
            const pluginArrayProxy = new Proxy(originalPlugins, {
                get(target, prop) {
                    if (prop === 'length') return 1;
                    if (prop === '0') return pdfPlugin;
                    if (prop === 'item') return function(index) { return index === 0 ? pdfPlugin : null; };
                    if (prop === 'namedItem') return function(name) { return name === 'PDF Viewer' ? pdfPlugin : null; };
                    if (prop === 'refresh') return function() {};
                    return target[prop];
                }
            });
            
            Object.defineProperty(navigator, 'plugins', {
                get: () => pluginArrayProxy,
                configurable: true
            });
            
            console.log('[Turnstile Bypass] ✅ Plugins fixed with Proxy');
        }
    } catch (e) {
        console.warn('[Turnstile Bypass] ⚠️ Plugins fix failed:', e);
    }
    
    // ============================================================================
    // 第 5 部分：修复 iframe 检测（Turnstile 会检查）
    // ============================================================================
    
    // 确保 window.top === window.self（不在 iframe 中）
    try {
        Object.defineProperty(window, 'top', {
            get: () => window,
            configurable: true
        });
        
        Object.defineProperty(window, 'self', {
            get: () => window,
            configurable: true
        });
        
        console.log('[Turnstile Bypass] ✅ iframe detection bypassed');
    } catch (e) {}
    
    // ============================================================================
    // 第 6 部分：修复 Error.stack 格式（Cloudflare 会检查）
    // ============================================================================
    
    try {
        const originalError = Error;
        const originalPrepareStackTrace = Error.prepareStackTrace;
        
        Error = function(...args) {
            const err = new originalError(...args);
            
            // 修复 stack 格式，移除自动化痕迹
            if (err.stack) {
                err.stack = err.stack
                    .replace(/at __puppeteer_evaluation_script__/g, 'at <anonymous>')
                    .replace(/at __playwright_evaluation_script__/g, 'at <anonymous>')
                    .replace(/at Object\.callFunctionOn/g, 'at <anonymous>')
                    .replace(/at ExecutionContext\.evaluateHandle/g, 'at <anonymous>');
            }
            
            return err;
        };
        
        Error.prototype = originalError.prototype;
        Error.prepareStackTrace = originalPrepareStackTrace;
        
        console.log('[Turnstile Bypass] ✅ Error.stack format fixed');
    } catch (e) {}
    
    // ============================================================================
    // 第 7 部分：添加真实的用户交互痕迹
    // ============================================================================
    
    // 模拟鼠标移动（Turnstile 会检查）
    let mouseX = 0;
    let mouseY = 0;
    let lastMouseMove = Date.now();
    
    document.addEventListener('mousemove', function(e) {
        mouseX = e.clientX;
        mouseY = e.clientY;
        lastMouseMove = Date.now();
    }, true);
    
    // 注入假的鼠标移动历史
    Object.defineProperty(window, '__mouseHistory', {
        get: () => ({
            x: mouseX,
            y: mouseY,
            lastMove: lastMouseMove,
            hasMoved: Date.now() - lastMouseMove < 5000
        }),
        configurable: true
    });
    
    console.log('[Turnstile Bypass] ✅ Mouse interaction simulation added');
    
    // ============================================================================
    // 第 8 部分：修复 Performance API（Cloudflare 会检查）
    // ============================================================================
    
    if (window.performance && window.performance.getEntriesByType) {
        const originalGetEntriesByType = window.performance.getEntriesByType;
        
        window.performance.getEntriesByType = function(type) {
            const entries = originalGetEntriesByType.call(this, type);
            
            // 确保有 navigation 条目（真实浏览器必有）
            if (type === 'navigation' && entries.length === 0) {
                return [{
                    name: document.location.href,
                    entryType: 'navigation',
                    startTime: 0,
                    duration: Math.random() * 1000,
                    initiatorType: 'navigation',
                    nextHopProtocol: 'h2',
                    workerStart: 0,
                    redirectStart: 0,
                    redirectEnd: 0,
                    fetchStart: Math.random() * 100,
                    domainLookupStart: Math.random() * 100,
                    domainLookupEnd: Math.random() * 100,
                    connectStart: Math.random() * 100,
                    connectEnd: Math.random() * 100,
                    secureConnectionStart: Math.random() * 100,
                    requestStart: Math.random() * 100,
                    responseStart: Math.random() * 100,
                    responseEnd: Math.random() * 1000,
                    transferSize: Math.floor(Math.random() * 100000),
                    encodedBodySize: Math.floor(Math.random() * 50000),
                    decodedBodySize: Math.floor(Math.random() * 50000),
                    serverTiming: [],
                    unloadEventStart: 0,
                    unloadEventEnd: 0,
                    domInteractive: Math.random() * 1000,
                    domContentLoadedEventStart: Math.random() * 1000,
                    domContentLoadedEventEnd: Math.random() * 1000,
                    domComplete: Math.random() * 2000,
                    loadEventStart: Math.random() * 2000,
                    loadEventEnd: Math.random() * 2000,
                    type: 'navigate',
                    redirectCount: 0
                }];
            }
            
            return entries;
        };
        
        console.log('[Turnstile Bypass] ✅ Performance API fixed');
    }
    
    // ============================================================================
    // 第 9 部分：Turnstile 专用 - 拦截验证请求
    // ============================================================================
    
    // 拦截 Turnstile 的验证请求，添加真实的浏览器指纹
    const originalFetch = window.fetch;
    window.fetch = function(...args) {
        const url = args[0];
        
        // 检测 Turnstile 验证请求
        if (typeof url === 'string' && url.includes('challenges.cloudflare.com')) {
            console.log('[Turnstile Bypass] 🎯 Intercepting Turnstile request:', url);
            
            // 添加真实的请求头
            if (args[1]) {
                args[1].headers = args[1].headers || {};
                
                // 根据当前平台动态设置 Client Hints
                const platform = navigator.platform || 'Win32';
                const isMobile = platform === 'iPhone' || platform === 'iPad' || platform === 'iPod';
                const platformName = platform === 'iPhone' || platform === 'iPad' || platform === 'iPod' ? 'iOS' : 
                                    platform === 'MacIntel' ? 'macOS' : 
                                    platform === 'Linux armv8l' ? 'Android' : 'Windows';
                
                // 提取 Chrome 版本
                const chromeMatch = navigator.userAgent.match(/Chrome\/(\d+)/);
                const chromeVersion = chromeMatch ? chromeMatch[1] : '141';
                
                args[1].headers['sec-ch-ua'] = `"Chromium";v="${chromeVersion}", "Google Chrome";v="${chromeVersion}", "Not-A.Brand";v="99"`;
                args[1].headers['sec-ch-ua-mobile'] = isMobile ? '?1' : '?0';
                args[1].headers['sec-ch-ua-platform'] = `"${platformName}"`;
                args[1].headers['sec-fetch-site'] = 'cross-site';
                args[1].headers['sec-fetch-mode'] = 'cors';
                args[1].headers['sec-fetch-dest'] = 'empty';
                
                console.log('[Turnstile Bypass] 📤 Request headers:', {
                    'sec-ch-ua': args[1].headers['sec-ch-ua'],
                    'sec-ch-ua-mobile': args[1].headers['sec-ch-ua-mobile'],
                    'sec-ch-ua-platform': args[1].headers['sec-ch-ua-platform']
                });
            }
        }
        
        return originalFetch.apply(this, args);
    };
    
    console.log('[Turnstile Bypass] ✅ Turnstile request interception enabled');
    
    // ============================================================================
    // 第 9.5 部分：Private Access Token (PAT) 支持
    // ============================================================================
    
    // 添加 PAT 相关的 API 支持（如果浏览器支持）
    try {
        // 检查是否支持 Private State Token API
        if (!document.hasPrivateToken) {
            // 模拟 hasPrivateToken API
            document.hasPrivateToken = function(issuer) {
                console.log('[Turnstile Bypass] 🔐 hasPrivateToken called for issuer:', issuer);
                // 返回 Promise，表示没有可用的 token（让 Cloudflare 使用其他验证方式）
                return Promise.resolve(false);
            };
            console.log('[Turnstile Bypass] ✅ Private Token API mocked');
        }
        
        // 添加 Credential Management API 支持
        if (navigator.credentials && !navigator.credentials.get.toString().includes('[native code]')) {
            const originalGet = navigator.credentials.get;
            navigator.credentials.get = function(options) {
                console.log('[Turnstile Bypass] 🔐 credentials.get called with options:', options);
                
                // 如果是 identity 请求（Private Access Token），返回 null
                if (options && options.identity) {
                    console.log('[Turnstile Bypass] 🔐 Blocking PAT request, returning null');
                    return Promise.resolve(null);
                }
                
                return originalGet.apply(this, arguments);
            };
        }
        
        console.log('[Turnstile Bypass] ✅ PAT support added');
    } catch (e) {
        console.warn('[Turnstile Bypass] ⚠️ PAT support failed:', e);
    }
    
    // ============================================================================
    // 第 10 部分：修复 toString 检测
    // ============================================================================
    
    // 确保所有被修改的函数的 toString() 返回原生代码
    const nativeToStringFunctionString = Error.toString().replace('Error', 'Function');
    
    const makeNativeString = (func) => {
        try {
            Object.defineProperty(func, 'toString', {
                value: () => nativeToStringFunctionString,
                configurable: true,
                writable: true
            });
        } catch (e) {}
    };
    
    // 应用到所有被修改的函数
    if (navigator.permissions && navigator.permissions.query) {
        makeNativeString(navigator.permissions.query);
    }
    if (window.fetch) {
        makeNativeString(window.fetch);
    }
    
    console.log('[Turnstile Bypass] ✅ toString detection bypassed');
    
    // ============================================================================
    // 完成
    // ============================================================================
    
    console.log('[Turnstile Bypass] ✅✅✅ All bypasses applied successfully!');
    console.log('[Turnstile Bypass] 📊 Summary:');
    console.log('  - Automation traces: REMOVED');
    console.log('  - Chrome object: ENHANCED');
    console.log('  - Plugins: FIXED');
    console.log('  - Permissions: PATCHED');
    console.log('  - Performance: FIXED');
    console.log('  - Error.stack: CLEANED');
    console.log('  - Turnstile requests: INTERCEPTED');
    console.log('  - toString: BYPASSED');
    
})();
