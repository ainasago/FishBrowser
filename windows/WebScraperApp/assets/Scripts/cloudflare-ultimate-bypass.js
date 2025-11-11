/**
 * Cloudflare Ultimate Bypass Script
 * 终极 Cloudflare 绕过脚本 - 移除所有自动化痕迹
 * 
 * 针对 "undetected chromedriver 1337!" 检测的完整解决方案
 */

(() => {
    'use strict';
    
    // ⭐ 静默模式 - 不输出任何日志
    // console.log('[Cloudflare Ultimate Bypass] Starting...');
    
    // ==================== 1. 移除 CDP Runtime 痕迹 ====================
    
    // 移除 CDP Runtime.enable 的痕迹
    if (window.cdc_adoQpoasnfa76pfcZLmcfl_Array) {
        delete window.cdc_adoQpoasnfa76pfcZLmcfl_Array;
    }
    if (window.cdc_adoQpoasnfa76pfcZLmcfl_Promise) {
        delete window.cdc_adoQpoasnfa76pfcZLmcfl_Promise;
    }
    if (window.cdc_adoQpoasnfa76pfcZLmcfl_Symbol) {
        delete window.cdc_adoQpoasnfa76pfcZLmcfl_Symbol;
    }
    
    // 移除所有 cdc_ 开头的属性
    Object.keys(window).forEach(key => {
        if (key.startsWith('cdc_')) {
            delete window[key];
        }
    });
    
    // ==================== 2. 移除 Webdriver 痕迹 ====================
    
    // 完全移除 navigator.webdriver
    Object.defineProperty(navigator, 'webdriver', {
        get: () => undefined,
        configurable: true,
        enumerable: false
    });
    
    // 移除 __webdriver_script_fn
    delete window.__webdriver_script_fn;
    delete window.__webdriver_evaluate;
    delete window.__selenium_evaluate;
    delete window.__webdriver_unwrapped;
    delete window.__driver_evaluate;
    delete window.__selenium_unwrapped;
    delete window.__fxdriver_evaluate;
    delete window.__driver_unwrapped;
    delete window.__fxdriver_unwrapped;
    delete window.__webdriver_script_func;
    
    // ==================== 3. 移除 Playwright/Puppeteer 痕迹 ====================
    
    delete window._playwright;
    delete window._playwrightInstance;
    delete window.__playwright;
    delete window.__pw_manual;
    delete window.__PW_inspect;
    
    delete window._puppeteer;
    delete window.__puppeteer;
    delete window.puppeteer;
    
    delete window._phantom;
    delete window.__phantom;
    delete window.callPhantom;
    delete window._selenium;
    delete window.__selenium;
    delete window.__nightmare;
    
    // ==================== 4. 修复 Chrome Runtime ====================
    
    // 确保 chrome.runtime 存在但不可访问（真实 Chrome 的行为）
    if (!window.chrome) {
        window.chrome = {};
    }
    
    // chrome.runtime 应该存在但访问会报错
    Object.defineProperty(window.chrome, 'runtime', {
        get: () => {
            throw new Error('Extension context invalidated.');
        },
        configurable: true,
        enumerable: true
    });
    
    // chrome.loadTimes 应该存在（真实 Chrome 有这个）
    if (!window.chrome.loadTimes) {
        window.chrome.loadTimes = function() {
            return {
                commitLoadTime: Date.now() / 1000 - Math.random() * 2,
                connectionInfo: 'http/1.1',
                finishDocumentLoadTime: Date.now() / 1000 - Math.random(),
                finishLoadTime: Date.now() / 1000 - Math.random() * 0.5,
                firstPaintAfterLoadTime: 0,
                firstPaintTime: Date.now() / 1000 - Math.random() * 1.5,
                navigationType: 'Other',
                npnNegotiatedProtocol: 'http/1.1',
                requestTime: Date.now() / 1000 - Math.random() * 3,
                startLoadTime: Date.now() / 1000 - Math.random() * 2.5,
                wasAlternateProtocolAvailable: false,
                wasFetchedViaSpdy: false,
                wasNpnNegotiated: false
            };
        };
    }
    
    // chrome.csi 应该存在
    if (!window.chrome.csi) {
        window.chrome.csi = function() {
            return {
                onloadT: Date.now(),
                pageT: Date.now() - performance.timing.navigationStart,
                startE: performance.timing.navigationStart,
                tran: 15
            };
        };
    }
    
    // ==================== 5. 修复 Permissions ====================
    
    const originalQuery = window.navigator.permissions.query;
    window.navigator.permissions.query = function(parameters) {
        if (parameters.name === 'notifications') {
            return Promise.resolve({
                state: Notification.permission,
                onchange: null
            });
        }
        return originalQuery.call(window.navigator.permissions, parameters);
    };
    
    // ==================== 6. 修复 Plugins ====================
    
    // 真实 Chrome 的 plugins 是空的
    Object.defineProperty(navigator, 'plugins', {
        get: () => {
            const plugins = [];
            plugins.length = 0;
            plugins.item = () => null;
            plugins.namedItem = () => null;
            plugins.refresh = () => {};
            return plugins;
        },
        configurable: true,
        enumerable: true
    });
    
    // ==================== 7. 修复 Languages ====================
    
    // 真实 Chrome 只有一个语言
    Object.defineProperty(navigator, 'languages', {
        get: () => [navigator.language || 'en-US'],
        configurable: true,
        enumerable: true
    });
    
    // ==================== 8. 移除 Automation 标志 ====================
    
    // 移除 domAutomation 和 domAutomationController
    delete window.domAutomation;
    delete window.domAutomationController;
    
    // ==================== 9. 修复 Connection Info ====================
    
    if (navigator.connection) {
        Object.defineProperty(navigator.connection, 'rtt', {
            get: () => 50 + Math.floor(Math.random() * 50),
            configurable: true
        });
        
        Object.defineProperty(navigator.connection, 'downlink', {
            get: () => 5 + Math.random() * 10,
            configurable: true
        });
        
        Object.defineProperty(navigator.connection, 'effectiveType', {
            get: () => '4g',
            configurable: true
        });
    }
    
    // ==================== 10. 修复 Battery API ====================
    
    if (navigator.getBattery) {
        const originalGetBattery = navigator.getBattery;
        navigator.getBattery = function() {
            return originalGetBattery.call(navigator).then(battery => {
                Object.defineProperty(battery, 'charging', {
                    get: () => true,
                    configurable: true
                });
                Object.defineProperty(battery, 'chargingTime', {
                    get: () => 0,
                    configurable: true
                });
                Object.defineProperty(battery, 'dischargingTime', {
                    get: () => Infinity,
                    configurable: true
                });
                Object.defineProperty(battery, 'level', {
                    get: () => 1,
                    configurable: true
                });
                return battery;
            });
        };
    }
    
    // ==================== 11. 修复 Screen ====================
    
    // 确保 screen 属性一致
    Object.defineProperty(screen, 'availTop', {
        get: () => 0,
        configurable: true
    });
    
    Object.defineProperty(screen, 'availLeft', {
        get: () => 0,
        configurable: true
    });
    
    // ==================== 12. 修复 Error Stack ====================
    
    // 确保 Error.stack 格式正常
    const OriginalError = window.Error;
    window.Error = function(...args) {
        const error = new OriginalError(...args);
        // 移除可能暴露自动化的堆栈信息
        if (error.stack) {
            error.stack = error.stack
                .replace(/at __puppeteer_evaluation_script__/g, 'at <anonymous>')
                .replace(/at __playwright_evaluation_script__/g, 'at <anonymous>')
                .replace(/at __webdriver_evaluation_script__/g, 'at <anonymous>');
        }
        return error;
    };
    window.Error.prototype = OriginalError.prototype;
    
    // ==================== 13. 修复 toString ====================
    
    // 确保所有重写的函数的 toString 返回正常
    const toStringProxy = new Proxy(Function.prototype.toString, {
        apply: function(target, thisArg, args) {
            if (thisArg === navigator.permissions.query) {
                return 'function query() { [native code] }';
            }
            if (thisArg === navigator.getBattery) {
                return 'function getBattery() { [native code] }';
            }
            if (thisArg === window.chrome.loadTimes) {
                return 'function loadTimes() { [native code] }';
            }
            if (thisArg === window.chrome.csi) {
                return 'function csi() { [native code] }';
            }
            return target.apply(thisArg, args);
        }
    });
    
    Function.prototype.toString = toStringProxy;
    
    // ==================== 14. 移除 Console 痕迹 ====================
    
    // 移除可能的 console 调试痕迹
    const originalConsoleDebug = console.debug;
    console.debug = function(...args) {
        // 过滤掉自动化相关的调试信息
        const message = args.join(' ');
        if (message.includes('DevTools') || 
            message.includes('puppeteer') || 
            message.includes('playwright') ||
            message.includes('webdriver')) {
            return;
        }
        return originalConsoleDebug.apply(console, args);
    };
    
    // ==================== 15. 修复 iframe 检测 ====================
    
    // 确保 window.top === window.self（除非真的在 iframe 中）
    try {
        if (window.top !== window.self) {
            // 真的在 iframe 中，不修改
        } else {
            Object.defineProperty(window, 'top', {
                get: () => window,
                configurable: true
            });
            Object.defineProperty(window, 'self', {
                get: () => window,
                configurable: true
            });
        }
    } catch(e) {
        // 跨域 iframe，忽略
    }
    
    // ==================== 16. 修复 Document 属性 ====================
    
    Object.defineProperty(document, 'hidden', {
        get: () => false,
        configurable: true
    });
    
    Object.defineProperty(document, 'visibilityState', {
        get: () => 'visible',
        configurable: true
    });
    
    // ==================== 17. 移除 Notification 痕迹 ====================
    
    // 确保 Notification.permission 正常
    if (window.Notification) {
        Object.defineProperty(Notification, 'permission', {
            get: () => 'default',
            configurable: true
        });
    }
    
    // ==================== 18. 修复 Performance ====================
    
    // 确保 performance.memory 存在
    if (!performance.memory) {
        Object.defineProperty(performance, 'memory', {
            get: () => ({
                jsHeapSizeLimit: 4294705152,
                totalJSHeapSize: 2944193 + Math.floor(Math.random() * 1000000),
                usedJSHeapSize: 1673613 + Math.floor(Math.random() * 500000)
            }),
            configurable: true
        });
    }
    
    // ==================== 19. 修复 Navigator 属性顺序 ====================
    
    // Cloudflare 会检查 navigator 属性的顺序
    // 确保关键属性按正确顺序出现
    const navigatorProps = [
        'vendorSub', 'productSub', 'vendor', 'maxTouchPoints', 'scheduling',
        'userActivation', 'doNotTrack', 'geolocation', 'connection', 'plugins',
        'mimeTypes', 'pdfViewerEnabled', 'webkitTemporaryStorage', 'webkitPersistentStorage',
        'hardwareConcurrency', 'cookieEnabled', 'appCodeName', 'appName', 'appVersion',
        'platform', 'product', 'userAgent', 'language', 'languages', 'onLine',
        'webdriver', 'getBattery', 'getGamepads', 'javaEnabled', 'sendBeacon', 'vibrate'
    ];
    
    // ==================== 20. 最终清理 ====================
    
    // 移除所有可能的自动化标记
    const automationMarkers = [
        '__webdriver_script_fn',
        '__driver_evaluate',
        '__webdriver_evaluate',
        '__selenium_evaluate',
        '__fxdriver_evaluate',
        '__driver_unwrapped',
        '__webdriver_unwrapped',
        '__selenium_unwrapped',
        '__fxdriver_unwrapped',
        '__webdriver_script_func',
        '__webdriver_script_function',
        '_Selenium_IDE_Recorder',
        '_selenium',
        'calledSelenium',
        '$cdc_asdjflasutopfhvcZLmcfl_',
        '$chrome_asyncScriptInfo',
        '__$webdriverAsyncExecutor',
        'webdriver',
        '__webdriverFunc',
        'domAutomation',
        'domAutomationController'
    ];
    
    automationMarkers.forEach(marker => {
        try {
            delete window[marker];
            delete document[marker];
            delete navigator[marker];
        } catch(e) {}
    });
    
    // ==================== 21. 修复 Intl API ====================
    
    // 确保 Intl.DateTimeFormat 正常工作
    const OriginalDateTimeFormat = Intl.DateTimeFormat;
    Intl.DateTimeFormat = function(...args) {
        return new OriginalDateTimeFormat(...args);
    };
    Intl.DateTimeFormat.prototype = OriginalDateTimeFormat.prototype;
    
    // ==================== 22. 修复 Crypto API ====================
    
    // 确保 crypto.getRandomValues 正常
    if (window.crypto && window.crypto.getRandomValues) {
        const originalGetRandomValues = window.crypto.getRandomValues.bind(window.crypto);
        Object.defineProperty(window.crypto, 'getRandomValues', {
            value: function(array) {
                return originalGetRandomValues(array);
            },
            configurable: true,
            enumerable: true,
            writable: true
        });
    }
    
    // ==================== 23. 移除 Playwright 特定的 CDP 会话 ====================
    
    // Playwright 使用 CDP 会话，这会留下痕迹
    // 我们需要隐藏这些痕迹
    if (window.chrome && window.chrome.webstore) {
        // 如果有 webstore，说明是真实 Chrome，保留
    } else {
        // 否则，伪造一个 webstore 对象
        if (window.chrome) {
            Object.defineProperty(window.chrome, 'webstore', {
                get: () => undefined,
                configurable: true,
                enumerable: true
            });
        }
    }
    
    // ==================== 24. 修复 MediaDevices ====================
    
    if (navigator.mediaDevices && navigator.mediaDevices.enumerateDevices) {
        const originalEnumerateDevices = navigator.mediaDevices.enumerateDevices.bind(navigator.mediaDevices);
        navigator.mediaDevices.enumerateDevices = function() {
            return originalEnumerateDevices().then(devices => {
                // 返回真实设备列表，但移除可能的自动化标记
                return devices.map(device => ({
                    deviceId: device.deviceId,
                    kind: device.kind,
                    label: device.label,
                    groupId: device.groupId,
                    toJSON: function() {
                        return {
                            deviceId: this.deviceId,
                            kind: this.kind,
                            label: this.label,
                            groupId: this.groupId
                        };
                    }
                }));
            });
        };
    }
    
    // ==================== 25. 修复 Geolocation API ====================
    
    if (navigator.geolocation) {
        const originalGetCurrentPosition = navigator.geolocation.getCurrentPosition;
        const originalWatchPosition = navigator.geolocation.watchPosition;
        
        navigator.geolocation.getCurrentPosition = function(success, error, options) {
            // 正常调用，但确保没有自动化痕迹
            return originalGetCurrentPosition.call(navigator.geolocation, success, error, options);
        };
        
        navigator.geolocation.watchPosition = function(success, error, options) {
            return originalWatchPosition.call(navigator.geolocation, success, error, options);
        };
    }
    
    // ==================== 26. 最终日志（静默模式） ====================
    
    // 不输出日志，避免暴露脚本存在
    // console.log('[Cloudflare Ultimate Bypass] ✅ All automation traces removed');
    
    // 静默完成
    window.__cloudflare_bypass_loaded__ = true;
    
})();
