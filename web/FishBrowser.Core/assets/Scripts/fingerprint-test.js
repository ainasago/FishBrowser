// ============================================================================
// 浏览器指纹测试脚本
// 用于验证所有关键属性是否正确设置
// ============================================================================

(function() {
    'use strict';
    
    console.log('========================================');
    console.log('🔍 Browser Fingerprint Test');
    console.log('========================================');
    
    const results = {
        passed: [],
        failed: [],
        warnings: []
    };
    
    // 测试 1: webdriver
    console.log('\n1️⃣ Testing webdriver...');
    if (navigator.webdriver === undefined) {
        results.passed.push('✅ webdriver is undefined (GOOD)');
    } else {
        results.failed.push(`❌ webdriver is ${navigator.webdriver} (BAD)`);
    }
    
    // 测试 2: platform & vendor 一致性
    console.log('\n2️⃣ Testing platform & vendor consistency...');
    const platform = navigator.platform;
    const vendor = navigator.vendor;
    const expectedVendor = (platform === 'iPhone' || platform === 'iPad' || platform === 'iPod' || platform === 'MacIntel') 
        ? 'Apple Computer, Inc.' 
        : 'Google Inc.';
    
    console.log(`   Platform: ${platform}`);
    console.log(`   Vendor: ${vendor}`);
    console.log(`   Expected: ${expectedVendor}`);
    
    if (vendor === expectedVendor) {
        results.passed.push(`✅ Vendor matches platform: ${platform} -> ${vendor}`);
    } else {
        results.failed.push(`❌ Vendor mismatch: ${platform} -> ${vendor} (expected ${expectedVendor})`);
    }
    
    // 测试 3: Chrome 对象
    console.log('\n3️⃣ Testing Chrome object...');
    if (window.chrome) {
        if (window.chrome.app) {
            results.passed.push('✅ chrome.app exists');
        } else {
            results.failed.push('❌ chrome.app missing');
        }
        
        if (typeof window.chrome.csi === 'function') {
            results.passed.push('✅ chrome.csi() exists');
        } else {
            results.failed.push('❌ chrome.csi() missing');
        }
        
        if (typeof window.chrome.loadTimes === 'function') {
            results.passed.push('✅ chrome.loadTimes() exists');
        } else {
            results.failed.push('❌ chrome.loadTimes() missing');
        }
        
        // chrome.runtime 不应该存在（真实 Chrome 没有）
        if (!window.chrome.runtime) {
            results.passed.push('✅ chrome.runtime does not exist (GOOD)');
        } else {
            results.warnings.push('⚠️ chrome.runtime exists (may indicate extension)');
        }
    } else {
        results.failed.push('❌ window.chrome missing');
    }
    
    // 测试 4: Plugins
    console.log('\n4️⃣ Testing plugins...');
    if (navigator.plugins && navigator.plugins.length > 0) {
        results.passed.push(`✅ Plugins exist (${navigator.plugins.length} plugins)`);
        console.log(`   Plugins: ${Array.from(navigator.plugins).map(p => p.name).join(', ')}`);
    } else {
        results.warnings.push('⚠️ No plugins found (may be suspicious)');
    }
    
    // 测试 5: Permissions API
    console.log('\n5️⃣ Testing Permissions API...');
    if (navigator.permissions && navigator.permissions.query) {
        results.passed.push('✅ Permissions API exists');
        
        // 测试 notifications 权限
        navigator.permissions.query({ name: 'notifications' }).then(result => {
            console.log(`   Notifications permission: ${result.state}`);
        }).catch(e => {
            console.warn('   Failed to query notifications permission:', e);
        });
    } else {
        results.failed.push('❌ Permissions API missing');
    }
    
    // 测试 6: Performance API
    console.log('\n6️⃣ Testing Performance API...');
    if (window.performance && window.performance.getEntriesByType) {
        const navigationEntries = window.performance.getEntriesByType('navigation');
        if (navigationEntries.length > 0) {
            results.passed.push('✅ Performance navigation entries exist');
        } else {
            results.warnings.push('⚠️ No performance navigation entries');
        }
    } else {
        results.failed.push('❌ Performance API missing');
    }
    
    // 测试 7: User-Agent
    console.log('\n7️⃣ Testing User-Agent...');
    const ua = navigator.userAgent;
    console.log(`   User-Agent: ${ua}`);
    
    if (ua.includes('HeadlessChrome')) {
        results.failed.push('❌ User-Agent contains "HeadlessChrome"');
    } else {
        results.passed.push('✅ User-Agent does not contain "HeadlessChrome"');
    }
    
    if (ua.includes('Chrome/')) {
        const chromeVersion = ua.match(/Chrome\/(\d+)/);
        if (chromeVersion) {
            const version = parseInt(chromeVersion[1]);
            if (version >= 100 && version <= 150) {
                results.passed.push(`✅ Chrome version is valid: ${version}`);
            } else {
                results.warnings.push(`⚠️ Chrome version may be outdated or invalid: ${version}`);
            }
        }
    }
    
    // 测试 8: Languages
    console.log('\n8️⃣ Testing languages...');
    if (navigator.languages && navigator.languages.length > 0) {
        results.passed.push(`✅ Languages exist: ${navigator.languages.join(', ')}`);
    } else {
        results.warnings.push('⚠️ No languages found');
    }
    
    // 测试 9: Hardware
    console.log('\n9️⃣ Testing hardware...');
    console.log(`   hardwareConcurrency: ${navigator.hardwareConcurrency}`);
    console.log(`   deviceMemory: ${navigator.deviceMemory}`);
    console.log(`   maxTouchPoints: ${navigator.maxTouchPoints}`);
    
    if (navigator.hardwareConcurrency >= 2 && navigator.hardwareConcurrency <= 32) {
        results.passed.push(`✅ hardwareConcurrency is reasonable: ${navigator.hardwareConcurrency}`);
    } else {
        results.warnings.push(`⚠️ hardwareConcurrency may be suspicious: ${navigator.hardwareConcurrency}`);
    }
    
    // 测试 10: 自动化痕迹
    console.log('\n🔟 Testing automation traces...');
    const automationProps = [
        '__webdriver_script_fn',
        '__driver_evaluate',
        '__playwright',
        '__pw_manual',
        '$cdc_asdjflasutopfhvcZLmcfl_',
        '_selenium'
    ];
    
    let foundTraces = 0;
    automationProps.forEach(prop => {
        if (window[prop] !== undefined) {
            results.failed.push(`❌ Automation trace found: ${prop}`);
            foundTraces++;
        }
    });
    
    if (foundTraces === 0) {
        results.passed.push('✅ No automation traces found');
    }
    
    // 输出结果
    console.log('\n========================================');
    console.log('📊 Test Results');
    console.log('========================================');
    
    console.log(`\n✅ Passed: ${results.passed.length}`);
    results.passed.forEach(msg => console.log(`   ${msg}`));
    
    if (results.warnings.length > 0) {
        console.log(`\n⚠️ Warnings: ${results.warnings.length}`);
        results.warnings.forEach(msg => console.log(`   ${msg}`));
    }
    
    if (results.failed.length > 0) {
        console.log(`\n❌ Failed: ${results.failed.length}`);
        results.failed.forEach(msg => console.log(`   ${msg}`));
    }
    
    // 总体评分
    const totalTests = results.passed.length + results.failed.length;
    const score = Math.round((results.passed.length / totalTests) * 100);
    
    console.log('\n========================================');
    console.log(`🎯 Overall Score: ${score}%`);
    
    if (score >= 90) {
        console.log('✅ Excellent! Browser fingerprint looks very natural.');
    } else if (score >= 70) {
        console.log('⚠️ Good, but there are some issues to fix.');
    } else {
        console.log('❌ Poor. Many issues detected. High risk of detection.');
    }
    
    console.log('========================================');
    
    // 返回结果供外部使用
    return {
        passed: results.passed.length,
        warnings: results.warnings.length,
        failed: results.failed.length,
        score: score
    };
})();
