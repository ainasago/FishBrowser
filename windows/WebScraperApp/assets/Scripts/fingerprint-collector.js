/**
 * 浏览器指纹收集器
 * 收集所有可能被 Cloudflare 检测的特征
 */

(() => {
    'use strict';
    
    const fingerprint = {
        timestamp: new Date().toISOString(),
        
        // ==================== 基本信息 ====================
        basic: {
            userAgent: navigator.userAgent,
            platform: navigator.platform,
            language: navigator.language,
            languages: navigator.languages,
            vendor: navigator.vendor,
            appVersion: navigator.appVersion,
            appName: navigator.appName,
            appCodeName: navigator.appCodeName,
            product: navigator.product,
            productSub: navigator.productSub,
            cookieEnabled: navigator.cookieEnabled,
            onLine: navigator.onLine,
            doNotTrack: navigator.doNotTrack
        },
        
        // ==================== 自动化检测 ====================
        automation: {
            webdriver: navigator.webdriver,
            _phantom: window._phantom !== undefined,
            _selenium: window._selenium !== undefined,
            callPhantom: window.callPhantom !== undefined,
            __nightmare: window.__nightmare !== undefined,
            __webdriver_script_fn: document.__webdriver_script_fn !== undefined,
            domAutomation: window.domAutomation !== undefined,
            domAutomationController: window.domAutomationController !== undefined,
            cdc_variables: Object.keys(window).filter(k => k.includes('cdc_')),
            chrome_runtime: window.chrome?.runtime !== undefined,
            chrome_loadTimes: typeof window.chrome?.loadTimes === 'function',
            chrome_csi: typeof window.chrome?.csi === 'function'
        },
        
        // ==================== 硬件信息 ====================
        hardware: {
            hardwareConcurrency: navigator.hardwareConcurrency,
            deviceMemory: navigator.deviceMemory,
            maxTouchPoints: navigator.maxTouchPoints,
            connection: navigator.connection ? {
                effectiveType: navigator.connection.effectiveType,
                rtt: navigator.connection.rtt,
                downlink: navigator.connection.downlink,
                saveData: navigator.connection.saveData
            } : null
        },
        
        // ==================== 插件和 MIME 类型 ====================
        plugins: {
            count: navigator.plugins.length,
            list: Array.from(navigator.plugins).map(p => ({
                name: p.name,
                filename: p.filename,
                description: p.description,
                length: p.length
            }))
        },
        
        mimeTypes: {
            count: navigator.mimeTypes.length,
            list: Array.from(navigator.mimeTypes).map(m => ({
                type: m.type,
                suffixes: m.suffixes,
                description: m.description
            }))
        },
        
        // ==================== Screen 信息 ====================
        screen: {
            width: screen.width,
            height: screen.height,
            availWidth: screen.availWidth,
            availHeight: screen.availHeight,
            colorDepth: screen.colorDepth,
            pixelDepth: screen.pixelDepth,
            orientation: screen.orientation?.type,
            devicePixelRatio: window.devicePixelRatio
        },
        
        // ==================== Canvas 指纹 ====================
        canvas: (() => {
            try {
                const canvas = document.createElement('canvas');
                const ctx = canvas.getContext('2d');
                canvas.width = 200;
                canvas.height = 50;
                
                ctx.textBaseline = 'top';
                ctx.font = '14px Arial';
                ctx.fillStyle = '#f60';
                ctx.fillRect(125, 1, 62, 20);
                ctx.fillStyle = '#069';
                ctx.fillText('Cloudflare Test 🎨', 2, 15);
                ctx.fillStyle = 'rgba(102, 204, 0, 0.7)';
                ctx.fillText('Cloudflare Test 🎨', 4, 17);
                
                const dataURL = canvas.toDataURL();
                const hash = dataURL.substring(dataURL.length - 100);
                
                return {
                    hash: hash,
                    length: dataURL.length
                };
            } catch (e) {
                return { error: e.message };
            }
        })(),
        
        // ==================== WebGL 信息 ====================
        webgl: (() => {
            try {
                const canvas = document.createElement('canvas');
                const gl = canvas.getContext('webgl') || canvas.getContext('experimental-webgl');
                
                if (!gl) return { supported: false };
                
                const debugInfo = gl.getExtension('WEBGL_debug_renderer_info');
                
                return {
                    supported: true,
                    vendor: gl.getParameter(gl.VENDOR),
                    renderer: gl.getParameter(gl.RENDERER),
                    version: gl.getParameter(gl.VERSION),
                    shadingLanguageVersion: gl.getParameter(gl.SHADING_LANGUAGE_VERSION),
                    unmaskedVendor: debugInfo ? gl.getParameter(debugInfo.UNMASKED_VENDOR_WEBGL) : null,
                    unmaskedRenderer: debugInfo ? gl.getParameter(debugInfo.UNMASKED_RENDERER_WEBGL) : null,
                    extensions: gl.getSupportedExtensions(),
                    maxTextureSize: gl.getParameter(gl.MAX_TEXTURE_SIZE),
                    maxViewportDims: gl.getParameter(gl.MAX_VIEWPORT_DIMS),
                    maxRenderbufferSize: gl.getParameter(gl.MAX_RENDERBUFFER_SIZE),
                    aliasedLineWidthRange: gl.getParameter(gl.ALIASED_LINE_WIDTH_RANGE),
                    aliasedPointSizeRange: gl.getParameter(gl.ALIASED_POINT_SIZE_RANGE)
                };
            } catch (e) {
                return { error: e.message };
            }
        })(),
        
        // ==================== WebGPU 信息 ====================
        webgpu: {
            supported: navigator.gpu !== undefined
        },
        
        // ==================== AudioContext 指纹 ====================
        audio: (() => {
            try {
                const AudioContext = window.AudioContext || window.webkitAudioContext;
                if (!AudioContext) return { supported: false };
                
                const context = new AudioContext();
                const oscillator = context.createOscillator();
                const analyser = context.createAnalyser();
                const gainNode = context.createGain();
                const scriptProcessor = context.createScriptProcessor(4096, 1, 1);
                
                gainNode.gain.value = 0;
                oscillator.connect(analyser);
                analyser.connect(scriptProcessor);
                scriptProcessor.connect(gainNode);
                gainNode.connect(context.destination);
                
                oscillator.start(0);
                
                return new Promise(resolve => {
                    scriptProcessor.onaudioprocess = function(event) {
                        const output = event.outputBuffer.getChannelData(0);
                        const hash = Array.from(output.slice(0, 30)).reduce((a, b) => a + b, 0);
                        
                        oscillator.stop();
                        scriptProcessor.disconnect();
                        gainNode.disconnect();
                        analyser.disconnect();
                        oscillator.disconnect();
                        
                        resolve({
                            supported: true,
                            sampleRate: context.sampleRate,
                            state: context.state,
                            maxChannelCount: context.destination.maxChannelCount,
                            numberOfInputs: scriptProcessor.numberOfInputs,
                            numberOfOutputs: scriptProcessor.numberOfOutputs,
                            channelCount: scriptProcessor.channelCount,
                            hash: hash
                        });
                    };
                });
            } catch (e) {
                return { error: e.message };
            }
        })(),
        
        // ==================== 字体检测 ====================
        fonts: (() => {
            const baseFonts = ['monospace', 'sans-serif', 'serif'];
            const testFonts = [
                'Arial', 'Verdana', 'Times New Roman', 'Courier New',
                'Georgia', 'Palatino', 'Garamond', 'Bookman',
                'Comic Sans MS', 'Trebuchet MS', 'Impact'
            ];
            
            const canvas = document.createElement('canvas');
            const ctx = canvas.getContext('2d');
            
            const baseSizes = {};
            baseFonts.forEach(baseFont => {
                ctx.font = `72px ${baseFont}`;
                baseSizes[baseFont] = ctx.measureText('mmmmmmmmmmlli').width;
            });
            
            const detectedFonts = [];
            testFonts.forEach(testFont => {
                let detected = false;
                baseFonts.forEach(baseFont => {
                    ctx.font = `72px ${testFont}, ${baseFont}`;
                    const size = ctx.measureText('mmmmmmmmmmlli').width;
                    if (size !== baseSizes[baseFont]) {
                        detected = true;
                    }
                });
                if (detected) {
                    detectedFonts.push(testFont);
                }
            });
            
            return {
                detected: detectedFonts,
                count: detectedFonts.length
            };
        })(),
        
        // ==================== 时区信息 ====================
        timezone: {
            offset: new Date().getTimezoneOffset(),
            timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
            locale: Intl.DateTimeFormat().resolvedOptions().locale
        },
        
        // ==================== 权限 API ====================
        permissions: {
            notification: Notification.permission
        },
        
        // ==================== Battery API ====================
        battery: navigator.getBattery ? 'supported' : 'not_supported',
        
        // ==================== MediaDevices ====================
        mediaDevices: {
            supported: navigator.mediaDevices !== undefined,
            enumerateDevices: navigator.mediaDevices?.enumerateDevices !== undefined
        },
        
        // ==================== ServiceWorker ====================
        serviceWorker: {
            supported: navigator.serviceWorker !== undefined
        },
        
        // ==================== Bluetooth ====================
        bluetooth: {
            supported: navigator.bluetooth !== undefined
        },
        
        // ==================== USB ====================
        usb: {
            supported: navigator.usb !== undefined
        },
        
        // ==================== Speech Synthesis ====================
        speechSynthesis: {
            supported: window.speechSynthesis !== undefined,
            voicesCount: window.speechSynthesis?.getVoices().length || 0
        },
        
        // ==================== Performance ====================
        performance: {
            memory: performance.memory ? {
                jsHeapSizeLimit: performance.memory.jsHeapSizeLimit,
                totalJSHeapSize: performance.memory.totalJSHeapSize,
                usedJSHeapSize: performance.memory.usedJSHeapSize
            } : null,
            navigation: {
                type: performance.navigation?.type,
                redirectCount: performance.navigation?.redirectCount
            },
            timing: performance.timing ? {
                navigationStart: performance.timing.navigationStart,
                loadEventEnd: performance.timing.loadEventEnd,
                domContentLoadedEventEnd: performance.timing.domContentLoadedEventEnd
            } : null
        },
        
        // ==================== Window 属性 ====================
        window: {
            innerWidth: window.innerWidth,
            innerHeight: window.innerHeight,
            outerWidth: window.outerWidth,
            outerHeight: window.outerHeight,
            screenX: window.screenX,
            screenY: window.screenY,
            pageXOffset: window.pageXOffset,
            pageYOffset: window.pageYOffset
        },
        
        // ==================== Document 属性 ====================
        document: {
            characterSet: document.characterSet,
            compatMode: document.compatMode,
            documentMode: document.documentMode,
            hidden: document.hidden,
            visibilityState: document.visibilityState
        },
        
        // ==================== Error Stack ====================
        errorStack: (() => {
            try {
                throw new Error('test');
            } catch (e) {
                return {
                    stack: e.stack,
                    stackLength: e.stack?.length || 0
                };
            }
        })()
    };
    
    // 等待 AudioContext 异步结果
    if (fingerprint.audio instanceof Promise) {
        fingerprint.audio.then(result => {
            fingerprint.audio = result;
            console.log('🔍 浏览器指纹收集完成（含 Audio）');
            console.log(JSON.stringify(fingerprint, null, 2));
        });
    } else {
        console.log('🔍 浏览器指纹收集完成');
        console.log(JSON.stringify(fingerprint, null, 2));
    }
    
    // 存储到 window 对象供外部访问
    window.__fingerprint__ = fingerprint;
    
    return fingerprint;
})();
