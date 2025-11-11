// ============================================================================
// Cloudflare 验证等待助手
// 自动检测并等待 Cloudflare 验证完成
// ============================================================================

(function() {
    'use strict';
    
    console.log('[CF Wait Helper] 🕐 Starting Cloudflare verification monitor...');
    
    // 检测 Cloudflare 验证状态
    function isCloudflareChallenge() {
        // 检查 URL
        if (window.location.href.includes('challenges.cloudflare.com')) {
            return true;
        }
        
        // 检查页面内容
        const bodyText = document.body ? document.body.innerText : '';
        if (bodyText.includes('Checking your browser') || 
            bodyText.includes('Just a moment') ||
            bodyText.includes('Please wait')) {
            return true;
        }
        
        // 检查 Turnstile iframe
        const turnstileIframe = document.querySelector('iframe[src*="challenges.cloudflare.com"]');
        if (turnstileIframe) {
            return true;
        }
        
        return false;
    }
    
    // 等待验证完成
    function waitForVerification() {
        return new Promise((resolve, reject) => {
            let checkCount = 0;
            const maxChecks = 60; // 最多等待 60 秒
            
            const checkInterval = setInterval(() => {
                checkCount++;
                
                if (!isCloudflareChallenge()) {
                    clearInterval(checkInterval);
                    console.log('[CF Wait Helper] ✅ Cloudflare verification completed!');
                    resolve(true);
                    return;
                }
                
                if (checkCount >= maxChecks) {
                    clearInterval(checkInterval);
                    console.warn('[CF Wait Helper] ⚠️ Verification timeout after 60 seconds');
                    reject(new Error('Verification timeout'));
                    return;
                }
                
                // 每 5 秒输出一次状态
                if (checkCount % 5 === 0) {
                    console.log(`[CF Wait Helper] ⏳ Still waiting... (${checkCount}s)`);
                }
            }, 1000);
        });
    }
    
    // 监听页面加载完成
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function() {
            if (isCloudflareChallenge()) {
                console.log('[CF Wait Helper] 🔍 Cloudflare challenge detected, waiting for completion...');
                waitForVerification().then(() => {
                    console.log('[CF Wait Helper] ✅ Page is ready!');
                }).catch(err => {
                    console.error('[CF Wait Helper] ❌ Verification failed:', err);
                });
            } else {
                console.log('[CF Wait Helper] ✅ No Cloudflare challenge detected');
            }
        });
    } else {
        if (isCloudflareChallenge()) {
            console.log('[CF Wait Helper] 🔍 Cloudflare challenge detected, waiting for completion...');
            waitForVerification().then(() => {
                console.log('[CF Wait Helper] ✅ Page is ready!');
            }).catch(err => {
                console.error('[CF Wait Helper] ❌ Verification failed:', err);
            });
        } else {
            console.log('[CF Wait Helper] ✅ No Cloudflare challenge detected');
        }
    }
    
    // 监听 Turnstile 事件
    window.addEventListener('message', function(event) {
        // 检查是否是 Turnstile 消息
        if (event.data && typeof event.data === 'string') {
            try {
                const data = JSON.parse(event.data);
                if (data.source === 'cloudflare-challenge') {
                    console.log('[CF Wait Helper] 📨 Turnstile message:', data);
                    
                    if (data.status === 'success') {
                        console.log('[CF Wait Helper] ✅ Turnstile verification succeeded!');
                    } else if (data.status === 'error') {
                        console.error('[CF Wait Helper] ❌ Turnstile verification failed:', data.error);
                    }
                }
            } catch (e) {
                // 不是 JSON 消息，忽略
            }
        }
    });
    
    // 导出等待函数供外部使用
    window.waitForCloudflare = waitForVerification;
    
    console.log('[CF Wait Helper] ✅ Monitor initialized');
    
})();
