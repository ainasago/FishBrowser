/**
 * Cloudflare Turnstile 防检测脚本
 * 包含 30 项防检测措施，专门针对 Cloudflare Turnstile 验证
 * 
 * 使用方法：
 * await context.AddInitScriptAsync(File.ReadAllText("Assets/Scripts/cloudflare-anti-detection.js"));
 */

(() => {
    'use strict';
    
    // ==================== Navigator 伪装 ====================
    
    // 1. webdriver 配置（根据用户设置动态调整）
    // 从全局变量读取配置（由 C# 注入）
    const webdriverMode = window.__WEBDRIVER_MODE__ || 'undefined';
    
    if (webdriverMode === 'delete' || webdriverMode === 'undefined') {
        // 完全移除 webdriver 属性
        try {
            delete Object.getPrototypeOf(navigator).webdriver;
        } catch(e) {}
        
        try {
            delete navigator.__proto__.webdriver;
        } catch(e) {}
        
        try {
            delete navigator.webdriver;
        } catch(e) {}
        
        // 强制重定义为 undefined
        Object.defineProperty(navigator, 'webdriver', { 
            get: () => undefined,
            set: () => {},
            configurable: true,
            enumerable: false
        });
    } else if (webdriverMode === 'true') {
        // 设置为 true（与真实 Chrome 一致）
        Object.defineProperty(navigator, 'webdriver', { 
            get: () => true,
            configurable: true,
            enumerable: true
        });
    } else if (webdriverMode === 'false') {
        // 设置为 false
        Object.defineProperty(navigator, 'webdriver', { 
            get: () => false,
            configurable: true,
            enumerable: true
        });
    }
    
    // 2. 伪装 plugins（与真实 Chrome 141 一致）
    // 注意：现代 Chrome 的 plugins 通常为空或只有 PDF
    // 为了避免被检测，我们保持为空（与真实 Chrome 141 一致）
    Object.defineProperty(navigator, 'plugins', {
        get: () => {
            const plugins = [];
            plugins.length = 0;
            plugins.item = function(index) { return null; };
            plugins.namedItem = function(name) { return null; };
            plugins.refresh = function() {};
            return plugins;
        },
        configurable: true
    });
    
    // 3. 伪装 mimeTypes
    const mimeTypeData = [
        { type: 'application/pdf', suffixes: 'pdf', description: 'Portable Document Format' },
        { type: 'text/pdf', suffixes: 'pdf', description: 'Portable Document Format' }
    ];
    Object.defineProperty(navigator, 'mimeTypes', {
        get: () => mimeTypeData,
        configurable: true
    });
    
    // 4. 伪装 languages（只保留主语言，与真实 Chrome 一致）
    Object.defineProperty(navigator, 'languages', {
        get: () => ['zh-CN'],  // 真实 Chrome 只有一个语言
        configurable: true
    });
    
    // 5. 伪装 permissions（增强版）
    const originalPermissionsQuery = navigator.permissions.query;
    navigator.permissions.query = function(parameters) {
        if (parameters.name === 'notifications') {
            return Promise.resolve({ 
                state: Notification.permission,
                onchange: null 
            });
        }
        return originalPermissionsQuery.call(navigator.permissions, parameters);
    };
    
    // 6. 伪装 hardwareConcurrency（匹配真实 Chrome）
    Object.defineProperty(navigator, 'hardwareConcurrency', {
        get: () => 16,  // 真实 Chrome 是 16 核
        configurable: true
    });
    
    // 7. 伪装 deviceMemory
    Object.defineProperty(navigator, 'deviceMemory', {
        get: () => 8,
        configurable: true
    });
    
    // 8. 伪装 maxTouchPoints（匹配真实 Chrome）
    Object.defineProperty(navigator, 'maxTouchPoints', {
        get: () => 10,  // 真实 Chrome 是 10（触摸屏）
        configurable: true
    });
    
    // 9. 伪装 connection（匹配真实 Chrome）
    Object.defineProperty(navigator, 'connection', {
        get: () => ({
            effectiveType: '4g',
            rtt: 200,  // 真实 Chrome 是 200ms
            downlink: 1.55,  // 真实 Chrome 的实际下载速度
            saveData: false,
            onchange: null,
            addEventListener: () => {},
            removeEventListener: () => {},
            dispatchEvent: () => true
        }),
        configurable: true
    });
    
    // 10. 伪装 platform（如果还没被 CDP 注入覆盖）
    const originalPlatform = navigator.platform;
    if (originalPlatform === 'Win32' || originalPlatform === 'MacIntel' || originalPlatform === 'Linux x86_64' || 
        originalPlatform === 'iPhone' || originalPlatform === 'iPad' || originalPlatform === 'Linux armv8l')
    {
        // 已经被 CDP 注入设置，不覆盖
    }
    else
    {
        Object.defineProperty(navigator, 'platform', {
            get: () => 'Win32',
            configurable: true
        });
    }
    
    // 11. 伪装 vendor（根据平台动态设置）
    // ⚠️ 重要：不要覆盖已经正确设置的 vendor（Turnstile 绕过脚本已经设置过了）
    console.log('[cloudflare-anti-detection.js] Checking vendor property...');
    console.log('[cloudflare-anti-detection.js] Current platform:', navigator.platform);
    console.log('[cloudflare-anti-detection.js] Current vendor:', navigator.vendor);
    
    // 检查 vendor 是否已经正确设置
    const currentPlatform = navigator.platform || 'Win32';
    const currentVendor = navigator.vendor;
    const expectedVendor = (currentPlatform === 'iPhone' || currentPlatform === 'iPad' || currentPlatform === 'iPod' || currentPlatform === 'MacIntel') 
        ? 'Apple Computer, Inc.' 
        : 'Google Inc.';
    
    // 只有在 vendor 不正确时才覆盖
    if (currentVendor !== expectedVendor) {
        console.log('[cloudflare-anti-detection.js] ⚠️ Vendor mismatch, fixing...');
        console.log(`[cloudflare-anti-detection.js]   Current: ${currentVendor}`);
        console.log(`[cloudflare-anti-detection.js]   Expected: ${expectedVendor}`);
        
        Object.defineProperty(navigator, 'vendor', {
            get: () => {
                // 根据 platform 动态返回正确的 vendor
                const platform = navigator.platform || 'Win32';
                let vendorValue;
                
                if (platform === 'iPhone' || platform === 'iPad' || platform === 'iPod' || platform === 'MacIntel') {
                    // iOS/macOS 设备 - Safari 使用 Apple
                    vendorValue = 'Apple Computer, Inc.';
                } else if (platform === 'Linux armv8l' || platform.startsWith('Linux')) {
                    // Android/Linux - Chrome 使用 Google
                    vendorValue = 'Google Inc.';
                } else {
                    // Windows/其他 - Chrome 使用 Google
                    vendorValue = 'Google Inc.';
                }
                
                return vendorValue;
            },
            configurable: true
        });
        
        console.log('[cloudflare-anti-detection.js] ✅ Vendor fixed:', navigator.vendor);
    } else {
        console.log('[cloudflare-anti-detection.js] ✅ Vendor already correct, skipping');
    }
    
    // 12. 伪装 appVersion（必须与 userAgent 一致）
    Object.defineProperty(navigator, 'appVersion', {
        get: () => '5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36',
        configurable: true
    });
    
    // ==================== Chrome 对象伪装 ====================
    
    // 13. 伪装 chrome 对象（真实 Chrome 没有 runtime）
    if (!window.chrome) {
        window.chrome = {};
    }
    
    // 不要添加 chrome.runtime，真实 Chrome 没有这个属性
    // window.chrome.runtime = {
    //     connect: () => {},
    //     sendMessage: () => {},
    //     onMessage: { 
    //         addListener: () => {}, 
    //         removeListener: () => {} 
    //     }
    // };
    
    window.chrome.loadTimes = () => ({
        commitLoadTime: Date.now() / 1000 - Math.random() * 2,
        connectionInfo: 'h2',
        finishDocumentLoadTime: Date.now() / 1000 - Math.random(),
        finishLoadTime: Date.now() / 1000 - Math.random(),
        firstPaintAfterLoadTime: 0,
        firstPaintTime: Date.now() / 1000 - Math.random() * 2,
        navigationType: 'Other',
        npnNegotiatedProtocol: 'h2',
        requestTime: Date.now() / 1000 - Math.random() * 3,
        startLoadTime: Date.now() / 1000 - Math.random() * 3,
        wasAlternateProtocolAvailable: false,
        wasFetchedViaSpdy: true,
        wasNpnNegotiated: true
    });
    
    window.chrome.csi = () => ({
        onloadT: Date.now(),
        pageT: Math.random() * 1000,
        startE: Date.now() - Math.random() * 3000,
        tran: 15
    });
    
    // ==================== 指纹伪造 ====================
    
    // 14. Canvas 指纹伪造（优化版，避免性能警告）
    const originalToDataURL = HTMLCanvasElement.prototype.toDataURL;
    const originalGetImageData = CanvasRenderingContext2D.prototype.getImageData;
    const processedCanvases = new WeakSet();
    
    const addCanvasNoise = (canvas, context) => {
        if (processedCanvases.has(canvas)) return;
        processedCanvases.add(canvas);
        
        try {
            const imageData = originalGetImageData.call(context, 0, 0, canvas.width, canvas.height);
            const data = imageData.data;
            // 只修改少量像素，更难被检测
            for (let i = 0; i < data.length; i += 40) {
                data[i] = data[i] ^ 1;
            }
            context.putImageData(imageData, 0, 0);
        } catch (e) {
            // 忽略错误
        }
    };
    
    HTMLCanvasElement.prototype.toDataURL = function() {
        if (this.width > 0 && this.height > 0 && this.width < 10000 && this.height < 10000) {
            const context = this.getContext('2d', { willReadFrequently: true });
            if (context) addCanvasNoise(this, context);
        }
        return originalToDataURL.apply(this, arguments);
    };
    
    // 15. WebGL 指纹伪造
    const getParameter = WebGLRenderingContext.prototype.getParameter;
    WebGLRenderingContext.prototype.getParameter = function(parameter) {
        if (parameter === 37445) {  // UNMASKED_VENDOR_WEBGL
            return 'Intel Inc.';
        }
        if (parameter === 37446) {  // UNMASKED_RENDERER_WEBGL
            return 'Intel Iris OpenGL Engine';
        }
        return getParameter.call(this, parameter);
    };
    
    // 16. AudioContext 指纹伪造
    const AudioContext = window.AudioContext || window.webkitAudioContext;
    if (AudioContext) {
        const originalCreateAnalyser = AudioContext.prototype.createAnalyser;
        AudioContext.prototype.createAnalyser = function() {
            const analyser = originalCreateAnalyser.call(this);
            const originalGetFloatFrequencyData = analyser.getFloatFrequencyData;
            analyser.getFloatFrequencyData = function(array) {
                originalGetFloatFrequencyData.call(this, array);
                for (let i = 0; i < array.length; i++) {
                    array[i] = array[i] + Math.random() * 0.0001;
                }
            };
            return analyser;
        };
    }
    
    // ==================== Screen 伪装 ====================
    
    // 17. Screen 属性（允许设备模拟覆盖，使用 configurable: true）
    // 默认值为桌面尺寸，但移动设备模拟会覆盖这些值
    Object.defineProperty(screen, 'availWidth', { get: () => 1280, configurable: true });
    Object.defineProperty(screen, 'availHeight', { get: () => 720, configurable: true });
    Object.defineProperty(screen, 'width', { get: () => 1280, configurable: true });
    Object.defineProperty(screen, 'height', { get: () => 720, configurable: true });
    Object.defineProperty(screen, 'colorDepth', { get: () => 24, configurable: true });
    Object.defineProperty(screen, 'pixelDepth', { get: () => 24, configurable: true });
    
    // ==================== 时区伪装 ====================
    
    // 18. Date.prototype.getTimezoneOffset
    Date.prototype.getTimezoneOffset = function() {
        return -480;  // UTC+8 (Asia/Shanghai)
    };
    
    // 19. Intl.DateTimeFormat
    const originalResolvedOptions = Intl.DateTimeFormat.prototype.resolvedOptions;
    Intl.DateTimeFormat.prototype.resolvedOptions = function() {
        const options = originalResolvedOptions.call(this);
        options.timeZone = 'Asia/Shanghai';
        return options;
    };
    
    // ==================== Notification 伪装 ====================
    
    // 20. Notification.permission
    Object.defineProperty(Notification, 'permission', {
        get: () => 'default',
        configurable: true
    });
    
    // ==================== Turnstile 专用 API 伪装 ====================
    
    // 21. Battery API
    if (!navigator.getBattery) {
        navigator.getBattery = () => Promise.resolve({
            charging: true,
            chargingTime: 0,
            dischargingTime: Infinity,
            level: 1,
            addEventListener: () => {},
            removeEventListener: () => {},
            dispatchEvent: () => true
        });
    }
    
    // 22. MediaDevices API
    if (navigator.mediaDevices && navigator.mediaDevices.enumerateDevices) {
        const originalEnumerateDevices = navigator.mediaDevices.enumerateDevices;
        navigator.mediaDevices.enumerateDevices = async function() {
            return [
                { deviceId: 'default', kind: 'audioinput', label: 'Default - Microphone', groupId: 'group1' },
                { deviceId: 'default', kind: 'audiooutput', label: 'Default - Speaker', groupId: 'group1' },
                { deviceId: 'default', kind: 'videoinput', label: 'Default - Camera', groupId: 'group2' }
            ];
        };
    }
    
    // 23. ServiceWorker API
    if (!navigator.serviceWorker) {
        Object.defineProperty(navigator, 'serviceWorker', {
            get: () => ({
                register: () => Promise.resolve(),
                getRegistrations: () => Promise.resolve([]),
                ready: Promise.resolve(),
                controller: null,
                addEventListener: () => {},
                removeEventListener: () => {}
            }),
            configurable: true
        });
    }
    
    // 24. Bluetooth API
    if (!navigator.bluetooth) {
        Object.defineProperty(navigator, 'bluetooth', {
            get: () => ({
                getAvailability: () => Promise.resolve(false),
                requestDevice: () => Promise.reject(new Error('Bluetooth adapter not available'))
            }),
            configurable: true
        });
    }
    
    // 25. USB API
    if (!navigator.usb) {
        Object.defineProperty(navigator, 'usb', {
            get: () => ({
                getDevices: () => Promise.resolve([]),
                requestDevice: () => Promise.reject(new Error('No device selected'))
            }),
            configurable: true
        });
    }
    
    // 26. Presentation API
    if (!navigator.presentation) {
        Object.defineProperty(navigator, 'presentation', {
            get: () => ({
                defaultRequest: null,
                receiver: null
            }),
            configurable: true
        });
    }
    
    // 27. Credentials API
    if (!navigator.credentials) {
        Object.defineProperty(navigator, 'credentials', {
            get: () => ({
                get: () => Promise.resolve(null),
                store: () => Promise.resolve(),
                create: () => Promise.resolve(null),
                preventSilentAccess: () => Promise.resolve()
            }),
            configurable: true
        });
    }
    
    // 28. Keyboard API
    if (!navigator.keyboard) {
        Object.defineProperty(navigator, 'keyboard', {
            get: () => ({
                getLayoutMap: () => Promise.resolve(new Map()),
                lock: () => Promise.resolve(),
                unlock: () => {}
            }),
            configurable: true
        });
    }
    
    // 29. MediaSession API
    if (!navigator.mediaSession) {
        Object.defineProperty(navigator, 'mediaSession', {
            get: () => ({
                metadata: null,
                playbackState: 'none',
                setActionHandler: () => {},
                setPositionState: () => {}
            }),
            configurable: true
        });
    }
    
    // ==================== 自动化痕迹移除 ====================
    
    // 30. 移除 Playwright/Puppeteer 痕迹
    delete window.cdc_adoQpoasnfa76pfcZLmcfl_Array;
    delete window.cdc_adoQpoasnfa76pfcZLmcfl_Promise;
    delete window.cdc_adoQpoasnfa76pfcZLmcfl_Symbol;
    
    // ==================== 完成 ====================
    
    console.log('✅ Cloudflare Turnstile 防检测脚本已加载（30 项措施）');
    console.log('📋 措施列表：');
    console.log('  - Navigator 伪装（12 项）');
    console.log('  - Chrome 对象伪装（3 项）');
    console.log('  - 指纹伪造（3 项：Canvas/WebGL/Audio）');
    console.log('  - Screen/时区/通知伪装（4 项）');
    console.log('  - Turnstile 专用 API（9 项）');
    console.log('  - 自动化痕迹移除（1 项）');
    
})();
