/**
 * Pre-Inject Fix Script
 * 在所有其他脚本之前运行，移除最早期的自动化痕迹
 * 必须通过 CDP Page.addScriptToEvaluateOnNewDocument 注入
 */

// ==================== 最早期的痕迹移除 ====================

// 1. 立即删除所有 cdc_ 变量（在任何代码运行前）
Object.keys(window).forEach(key => {
    if (key.startsWith('cdc_') || key.startsWith('$cdc_') || key.startsWith('__cdc_')) {
        try {
            delete window[key];
        } catch(e) {}
    }
});

// 2. 立即删除 webdriver（在任何检测前）
try {
    delete Object.getPrototypeOf(navigator).webdriver;
    delete navigator.__proto__.webdriver;
    delete navigator.webdriver;
} catch(e) {}

Object.defineProperty(navigator, 'webdriver', {
    get: () => undefined,
    configurable: true,
    enumerable: false
});

// 3. 立即删除所有自动化标记
const markers = [
    '__webdriver_script_fn',
    '__driver_evaluate',
    '__webdriver_evaluate',
    '__selenium_evaluate',
    '__fxdriver_evaluate',
    '_Selenium_IDE_Recorder',
    '_selenium',
    'calledSelenium',
    '$chrome_asyncScriptInfo',
    '__$webdriverAsyncExecutor',
    '__webdriverFunc',
    'domAutomation',
    'domAutomationController',
    '_playwright',
    '__playwright',
    '_puppeteer',
    '__puppeteer',
    '_phantom',
    '__phantom',
    'callPhantom',
    '__nightmare'
];

markers.forEach(marker => {
    try {
        delete window[marker];
        delete document[marker];
        delete navigator[marker];
    } catch(e) {}
});

// 4. 防止 Cloudflare 注入检测标记
const originalDefineProperty = Object.defineProperty;
Object.defineProperty = function(obj, prop, descriptor) {
    // 阻止 Cloudflare 定义检测属性
    if (prop === '__CF$cv$params' || prop === '__cfRLUnblockHandlers') {
        return obj;
    }
    return originalDefineProperty.call(this, obj, prop, descriptor);
};

// 5. 拦截 document.write（Cloudflare 可能用它注入检测代码）
const originalDocumentWrite = document.write;
document.write = function(content) {
    // 过滤掉包含 "undetected chromedriver" 的内容
    if (typeof content === 'string' && content.includes('undetected chromedriver')) {
        return;
    }
    return originalDocumentWrite.call(document, content);
};

// 6. 拦截 eval（防止动态注入检测代码）
const originalEval = window.eval;
window.eval = function(code) {
    if (typeof code === 'string' && (
        code.includes('undetected chromedriver') ||
        code.includes('cdc_') ||
        code.includes('__webdriver')
    )) {
        return;
    }
    return originalEval.call(window, code);
};

// 7. 静默完成（不输出任何日志）
window.__pre_inject_fix_loaded__ = true;
